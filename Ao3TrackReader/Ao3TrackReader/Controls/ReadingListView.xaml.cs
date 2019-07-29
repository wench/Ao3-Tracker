/*
Copyright 2017 Alexis Ryan

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using Ao3TrackReader.Resources;
using Ao3TrackReader.Data;
using Ao3TrackReader.Models;

namespace Ao3TrackReader.Controls
{
    public partial class ReadingListView : PaneView
    {
        public const int MaxRefreshTasks = 20;
        const int RefreshDelay = 200;

        GroupList2<Ao3PageViewModel> readingListBacking;

        public DisableableCommand AddToReadingListCommand { get; private set; }

        public MenuOpenLastCommand MenuOpenLastCommand => new MenuOpenLastCommand { ReadingList = this };
        public MenuOpenFullWorkLastCommand MenuOpenFullWorkLastCommand => new MenuOpenFullWorkLastCommand { ReadingList = this };
        public MenuOpenFullWorkCommand MenuOpenFullWorkCommand => new MenuOpenFullWorkCommand { ReadingList = this };

        public ReadingListView()
        {
            AddToReadingListCommand = new DisableableCommand(() => {
                if (AddPageButton.IsActive)
                    RemoveAsync(wvp.CurrentUri.AbsoluteUri);
                else
                    AddAsync(wvp.CurrentUri.AbsoluteUri);
            });

            InitializeComponent();

            readingListBacking = new GroupList2<Ao3PageViewModel>();

            bool b;

            App.Database.TryGetVariable("ReadingList.showTagsDefault", bool.TryParse, out b);
            TagsVisible = b;
            App.Database.TryGetVariable("ReadingList.showCompleteDefault", bool.TryParse, out b);
            readingListBacking.ShowHidden = b;

            ShowHiddenButton.IsActive = readingListBacking.ShowHidden;
            ShowTagsButton.IsActive = TagsVisible;

            tagTypeVisible = new Dictionary<Ao3TagType, bool>(3);

            App.Database.TryGetVariable("TagOptions.showCatTags", bool.TryParse, out b);
            tagTypeVisible[Ao3TagType.Category] = b;
            App.Database.TryGetVariable("TagOptions.showWIPTags", bool.TryParse, out b);
            tagTypeVisible[Ao3TagType.Complete] = b;
            App.Database.TryGetVariable("TagOptions.showRatingTags", bool.TryParse, out b);
            tagTypeVisible[Ao3TagType.Rating] = b;

            App.Database.GetVariableEvents("TagOptions.showCatTags").Updated += TagVisibilities_Updated;
            App.Database.GetVariableEvents("TagOptions.showWIPTags").Updated += TagVisibilities_Updated;
            App.Database.GetVariableEvents("TagOptions.showRatingTags").Updated += TagVisibilities_Updated;

        }

        protected override void OnWebViewPageSet()
        {
            base.OnWebViewPageSet();
            Device.BeginInvokeOnMainThread(() => RestoreReadingList());
        }

        TaskCompletionSource<bool> restored = new TaskCompletionSource<bool>();

        void RestoreReadingList()
        {
            Task.Factory.StartNew(async () =>
            {
                await App.Database.ReadingListCached.BeginDeferralAsync();
                try
                {

                    // Restore the reading list contents!
                    var items = new Dictionary<string, ReadingList>();
                    foreach (var i in await App.Database.ReadingListCached.SelectAsync())
                    {
                        items[i.Uri] = i;
                    }

                    var tasks = new Queue<Task>();

                    using (var tasklimit = new SemaphoreSlim(MaxRefreshTasks))
                    {
                        if (items.Count == 0)
                        {
                            tasks.Enqueue(AddAsyncImpl("http://archiveofourown.org/", DateTime.UtcNow.ToUnixTime()));
                        }
                        else
                        {
                            var timestamp = DateTime.UtcNow.ToUnixTime();
                            foreach (var item in items.Values)
                            {
                                var model = Ao3PageModel.Deserialize(item.Model) ?? Ao3SiteDataLookup.LookupQuick(item.Uri);
                                if (model == null) continue;

                                if (model.Uri.AbsoluteUri != item.Uri)
                                {
                                    await App.Database.ReadingListCached.DeleteAsync(item.Uri);
                                    await App.Database.ReadingListCached.InsertOrUpdateAsync(new ReadingList(model, timestamp, item.Unread));
                                }

                                if (readingListBacking.FindInAll((m) => m.Uri.AbsoluteUri == model.Uri.AbsoluteUri) is null)
                                {
                                    var viewmodel = new Ao3PageViewModel(model.Uri, model.HasChapters ? item.Unread : (int?)null, model.Type == Ao3PageType.Work ? tagTypeVisible : null)
                                    {
                                        TagsVisible = tags_visible,
                                        Favourite = item.Favourite
                                    };

                                    await wvp.DoOnMainThreadAsync(() =>
                                    {
                                        viewmodel.PropertyChanged += Viewmodel_PropertyChanged;
                                        readingListBacking.Add(viewmodel);
                                    });

                                    await tasklimit.WaitAsync();
                                    tasks.Enqueue(wvp.DoOnMainThreadAsync(async () =>
                                    {
                                        await viewmodel.SetBaseDataAsync(model,false);
                                        tasklimit.Release();
                                    }));
                                }

#pragma warning disable 4014
                                while (tasks.Count > 0 && tasks.Peek().IsCompleted)
                                    tasks.Dequeue();
#pragma warning restore 4014
                            }
                        }
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }

                    await wvp.DoOnMainThreadAsync(() =>
                    {
                        ListView.ItemsSource = readingListBacking;
                        restored.SetResult(true);
                        SyncIndicator.Content = new ActivityIndicator() { IsVisible = IsOnScreen, IsRunning = IsOnScreen, IsEnabled = IsOnScreen };
                        App.Database.GetVariableEvents("LogFontSizeUI").Updated += LogFontSizeUI_Updated;
                    });

                    GC.Collect();

                    // If we don't have network access, we wait till it's available
                    if (!App.Current.HaveNetwork)
                    {
                        tcsNetworkAvailable = new TaskCompletionSource<bool>();
                        App.Current.HaveNetworkChanged += App_HaveNetworkChanged;
                        await tcsNetworkAvailable.Task;
                    }

                    await SyncToServerAsync(false, true);

                    using (var tasklimit = new SemaphoreSlim(MaxRefreshTasks))
                    {
                        foreach (var viewmodel in readingListBacking.AllSafe)
                        {
                            await tasklimit.WaitAsync();

                            tasks.Enqueue(Task.Run(async () =>
                            {
                                await RefreshAsync(viewmodel);
                                await Task.Delay(RefreshDelay);
                                tasklimit.Release();
                            }));
#pragma warning disable 4014
                            while (tasks.Count > 0 && tasks.Peek().IsCompleted)
                                tasks.Dequeue();
#pragma warning restore 4014
                        }
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                        GC.Collect();
                    }
                }
                finally
                {
                    await App.Database.ReadingListCached.EndDeferralAsync().ConfigureAwait(false);
                }
                
                await wvp.DoOnMainThreadAsync(() =>
                {
                    RefreshButton.IsEnabled = true;
                    SyncIndicator.Content = null;
                }).ConfigureAwait(false);
            }, TaskCreationOptions.PreferFairness|TaskCreationOptions.LongRunning);
        }

        TaskCompletionSource<bool> tcsNetworkAvailable;
        private void App_HaveNetworkChanged(object sender, EventArgs<bool> e)
        {
            if (e)
            {
                App.Current.HaveNetworkChanged -= App_HaveNetworkChanged;
                tcsNetworkAvailable.SetResult(true);
            }
        }

        private void LogFontSizeUI_Updated(object sender, Ao3TrackDatabase.VariableUpdatedEventArgs e)
        {
            wvp.DoOnMainThreadAsync(() =>
            {
                ListView.ItemsSource = null;
                ListView.ItemsSource = readingListBacking;
            }).ConfigureAwait(false);
        }

        protected override void OnIsOnScreenChanging(bool newValue)
        {
            if (newValue == true)
            {
                ListView.Focus();
                if (SyncIndicator.Content != null)
                {
                    SyncIndicator.Content.IsEnabled = true;
                    SyncIndicator.Content.IsVisible = true;
                    SyncIndicator.Content.IsRunning = true;
                }
            }
            else
            {
                ListView.Unfocus();
                if (SyncIndicator.Content != null)
                {
                    SyncIndicator.Content.IsEnabled = false;
                    SyncIndicator.Content.IsVisible = false;
                    SyncIndicator.Content.IsRunning = false;
                }
            }
        }

        IEnumerable<Models.IHelpInfo> ButtonBarHelpItems
        {
            get
            {
                foreach (var v in buttonBar.Children)
                {
                    if (v is Models.IHelpInfo info)
                    {
                        if (!string.IsNullOrWhiteSpace(info.Text) && !string.IsNullOrWhiteSpace(info.Group))
                            yield return new Models.HelpInfoAdapter(info);
                    }
                }
            }
        }

        public IEnumerable<Models.IHelpInfo> HelpItems {
            get
            {
                var extraHelp = Resources["ExtraHelp"] as HelpInfo[];
                return extraHelp.Concat(ButtonBarHelpItems);
            }
        }

        Ao3PageViewModel selectedItem;
        private void UpdateSelectedItem(Ao3PageViewModel newselected)
        {
            var oldSelected = selectedItem;
            selectedItem = null;
            ListView.SelectedItem = null;

            if (!(oldSelected is null) && newselected != oldSelected) oldSelected.IsSelected = false;

            if (!(newselected is null))
            {
                newselected.IsSelected = true;
                selectedItem = newselected;
            }
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                if (selectedItem?.IsSelected != false && ListView.SelectedItem != selectedItem)
                {
                    try
                    {
                        ListView.SelectedItem = selectedItem;
                    }
                    catch
                    {
                    }
                }
            });
        }

        private void OnCellAppearing(object sender, EventArgs e)
        {
            var mi = ((Cell)sender);
            var item = mi.BindingContext as Ao3PageViewModel;
            if (item?.IsSelected == true)
            {
                UpdateSelectedItem(item);
            }
            mi.ForceUpdateSize();
        }

        private void OnItemTapped(object sender, ItemTappedEventArgs e)
        {           
            if (e.Item is Ao3PageViewModel item)
            {
                Goto(item,true,false);
            }
        }     

        private void OnMenuFavourite(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            if (mi.CommandParameter is Ao3PageViewModel item)
            {
                item.Favourite = !item.Favourite;
            }
        }

        private void OnMenuOpen(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            if (mi.CommandParameter is Ao3PageViewModel item)
            {
                Goto(item, false, false);
            }
        }

        private void OnMenuCopyLink(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            if (mi.CommandParameter is Ao3PageViewModel item)
            {
                wvp.CopyToClipboard(item.Uri.AbsoluteUri, "url");
                
            }
        }
        
        private async void OnMenuRefresh(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            if (mi.CommandParameter is Ao3PageViewModel item)
            {
                await RefreshAsync(item).ConfigureAwait(false);
            }

        }

        private void OnMenuDelete(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            if (mi.CommandParameter is Ao3PageViewModel item)
            {
                RemoveAsync(item.Uri.AbsoluteUri);
            }

        }

        private void OnRefresh(object sender, EventArgs e)
        {
            ListView.Focus();
            RefreshButton.IsEnabled = false;
            SyncIndicator.Content = new ActivityIndicator() { IsVisible = IsOnScreen, IsRunning = IsOnScreen, IsEnabled = IsOnScreen };

            Task.Factory.StartNew(async () =>
            {
                await App.Database.ReadingListCached.BeginDeferralAsync();
                try
                {
                    await SyncToServerAsync(false, true);

                    using (var tasklimit = new SemaphoreSlim(MaxRefreshTasks))
                    {
                        List<Task> tasks = new List<Task>();
                        foreach (var viewmodel in readingListBacking.AllSafe)
                        {
                            await tasklimit.WaitAsync();

                            tasks.Add(Task.Run(async () =>
                            {
                                await RefreshAsync(viewmodel);
                                await Task.Delay(RefreshDelay);
                                tasklimit.Release();
                            }));
                        }

                        await Task.WhenAll(tasks);
                    }
                }
                finally
                {
                    await App.Database.ReadingListCached.EndDeferralAsync().ConfigureAwait(false);
                }

                await wvp.DoOnMainThreadAsync(() =>
                {
                    RefreshButton.IsEnabled = true;
                    SyncIndicator.Content = null;
                    PageChange(wvp.CurrentUri);
                });
            }, TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning);
        }

        private void OnShowHidden(object sender, EventArgs e)
        {
            readingListBacking.ShowHidden = !readingListBacking.ShowHidden;
            ShowHiddenButton.IsActive = readingListBacking.ShowHidden;
        }

        bool tags_visible = false;
        Dictionary<Ao3TagType, bool> tagTypeVisible;

        public bool TagsVisible
        {
            get { return tags_visible; }
            set
            {
                if (tags_visible != value)
                {
                    tags_visible = value;
                    readingListBacking.ForEachInAll((item) => { item.TagsVisible = tags_visible; });
                }
            }
        }

        private void TagVisibilities_Updated(object sender, Ao3TrackDatabase.VariableUpdatedEventArgs e)
        {
            Ao3TagType type;
            switch (e.VarName)
            {
                case "TagOptions.showCatTags":
                    type = Ao3TagType.Category;
                    break;

                case "TagOptions.showWIPTags":
                    type = Ao3TagType.Complete;
                    break;

                case "TagOptions.showRatingTags":
                    type = Ao3TagType.Rating;
                    break;

                default:
                    return;
            }

            if (!bool.TryParse(e.NewValue, out var b))
                return;

            wvp.DoOnMainThreadAsync(() =>
            {
                tagTypeVisible[type] = b;
                readingListBacking.ForEachInAll((item) =>
                {
                    if (item?.BaseData?.Type == Ao3PageType.Work) item.SetTagVisibilities(type, b);
                });
            });
        }

        private void OnShowTags(object sender, EventArgs e)
        {
            TagsVisible = !TagsVisible;
            ShowTagsButton.IsActive = TagsVisible;
        }

        public void Goto(Ao3PageViewModel item, bool latest, bool fullwork)
        {
            if (item.BaseData.Type == Models.Ao3PageType.Work && !(item.BaseData.Details is null) && item.BaseData.Details.WorkId != 0)
            {
                if (latest) wvp.NavigateToLast(item.BaseData.Details.WorkId, fullwork);
                else wvp.Navigate(item.BaseData.Details.WorkId, fullwork);
            }
            else if (latest && item.BaseData.Type == Models.Ao3PageType.Series && item.BaseData.SeriesWorks?.Count > 0)
            {
                long workid = 0;

                foreach (var workmodel in item.BaseData.SeriesWorks)
                {
                    workid = workmodel.Details.WorkId;
                    int chapters_finished = 0;
                    if (item.WorkChapters.TryGetValue(workid, out var workchap))
                    {
                        chapters_finished = (int)workchap.number;
                        if (!(workchap.location is null)) { chapters_finished--; }
                    }
                    if (chapters_finished < workmodel.Details.Chapters.Available) break;
                }

                if (workid != 0) wvp.NavigateToLast(workid,false);
                else wvp.Navigate(item.Uri);
            }
            else
            {
                wvp.Navigate(item.Uri);
            }
            IsOnScreen = false;
        }

        public async void PageChange(Uri uri)
        {
            if (!(uri is null)) uri = Ao3SiteDataLookup.ReadingListlUri(uri.AbsoluteUri);

            await wvp.DoOnMainThreadAsync(() => 
            {
                if (uri is null)
                {
                    UpdateSelectedItem(null);
                    wvp.AddRemoveReadingListToolBarItem_IsActive = AddPageButton.IsActive = false;
                }
                else
                {
                    var item = readingListBacking.FindInAll((m) => m.HasUri(uri));
                    UpdateSelectedItem(item);
                    wvp.AddRemoveReadingListToolBarItem_IsActive = AddPageButton.IsActive = !(item is null);
                }
            }).ConfigureAwait(false);
        }

        public async Task SyncToServerAsync(bool newuser, bool dontrefresh=false)
        {
            if (!restored.Task.IsCompleted)
                await restored.Task;

            await App.Database.ReadingListCached.BeginDeferralAsync();
            try
            {
                var srl = new Models.ServerReadingList();
                if (!newuser && App.Database.TryGetVariable("ReadingList.last_sync", long.TryParse, out long last)) srl.last_sync = last;

                srl.paths = (await App.Database.ReadingListCached.SelectAsync()).ToDictionary(i => i.Uri, i => i.Timestamp);

                srl = await App.Storage.SyncReadingListAsync(srl);
                if (!(srl is null))
                {
                    using (var tasklimit = new SemaphoreSlim(MaxRefreshTasks))
                    {
                        var tasks = new Queue<Task>();
                        foreach (var item in srl.paths)
                        {
                            await tasklimit.WaitAsync();

                            tasks.Enqueue(Task.Run(async () =>
                            {
                                if (item.Value == -1)
                                {
                                    await RemoveAsyncImpl(item.Key);
                                }
                                else
                                {
                                    var viewmodel = await AddAsyncImpl(item.Key, item.Value);
                                    if (!dontrefresh && viewmodel != null) {
                                        await RefreshAsync(viewmodel);
                                        await Task.Delay(RefreshDelay);
                                    }
                                }
                                tasklimit.Release();
                            }));
#pragma warning disable 4014
                            while (tasks.Count > 0 && tasks.Peek().IsCompleted)
                                tasks.Dequeue();
#pragma warning restore 4014
                        }
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }
                    await App.Database.ReadingListCached.SaveVariableAsync("ReadingList.last_sync", srl.last_sync.ToString());
                }
            }
            finally
            {
                await App.Database.ReadingListCached.EndDeferralAsync().ConfigureAwait(false);
            }

            GC.Collect();
            PageChange(wvp?.CurrentUri);
        }

        public async void RemoveAsync(string href)
        {
            if (!restored.Task.IsCompleted)
                await restored.Task;

            await RemoveAsyncImpl(href);
            await Task.Factory.StartNew(() => SyncToServerAsync(false), TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness).ConfigureAwait(false);
        }

        async Task RemoveAsyncImpl(string href)
        {
            var uri = Ao3SiteDataLookup.ReadingListlUri(href);
            if (uri == null) return;
            await App.Database.ReadingListCached.DeleteAsync(uri.AbsoluteUri);
            var viewmodel = readingListBacking.FindInAll((m) => m.Uri == uri);
            if (viewmodel is null) return;
            viewmodel.PropertyChanged -= Viewmodel_PropertyChanged;
            await wvp.DoOnMainThreadAsync(() =>
            {
                if (viewmodel == selectedItem) UpdateSelectedItem(null);
                if (Ao3SiteDataLookup.ReadingListlUri(wvp.CurrentUri.AbsoluteUri) == viewmodel.Uri) wvp.AddRemoveReadingListToolBarItem_IsActive = AddPageButton.IsActive = false;
                readingListBacking.Remove(viewmodel);
                viewmodel.Dispose();
            });
        }

        public async void AddAsync(string href)
        {
            if (!restored.Task.IsCompleted)
                await restored.Task;

            var viewmodel = await AddAsyncImpl(href, DateTime.UtcNow.ToUnixTime());
            if (viewmodel != null) await RefreshAsync(viewmodel);
            await Task.Factory.StartNew(() => SyncToServerAsync(false), TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness).ConfigureAwait(false);
        }

        async Task<Ao3PageViewModel> AddAsyncImpl(string href, long timestamp)
        {
            if (!(readingListBacking.FindInAll((m) => m.Uri.AbsoluteUri == href) is null))
                return null;

            var model = Ao3SiteDataLookup.LookupQuick(href);
            if (model is null)
                return null;

            if (!(readingListBacking.FindInAll((m) => m.Uri.AbsoluteUri == model.Uri.AbsoluteUri) is null))
                return null;

            return await wvp.DoOnMainThreadAsync(async () =>
            {
                var viewmodel = new Ao3PageViewModel(model.Uri, model.HasChapters ? 0 : (int?)null, model.Type == Ao3PageType.Work ? tagTypeVisible : null) // Set unread to 0. this is to prevents UI locks when importing huge reading lists during syncs
                {
                    TagsVisible = tags_visible
                };
                viewmodel.PropertyChanged += Viewmodel_PropertyChanged;
                await viewmodel.SetBaseDataAsync(model, false);

                readingListBacking.Add(viewmodel);

                await App.Database.ReadingListCached.InsertOrUpdateAsync(new ReadingList(model, timestamp, viewmodel.ChaptersRead));

                var uri = Ao3SiteDataLookup.ReadingListlUri(wvp.CurrentUri.AbsoluteUri);
                if (uri == viewmodel.Uri) wvp.AddRemoveReadingListToolBarItem_IsActive = AddPageButton.IsActive = true;

                return viewmodel;
            });            
        }

        public async Task<IDictionary<string, bool>> AreUrlsInListAsync(string[] urls)
        {
            if (!restored.Task.IsCompleted)
                await restored.Task;

            return await Task.Run(() =>
            {
                var urlmap = new List<KeyValuePair<string, Uri>>();
                IDictionary<string, bool> result = new Dictionary<string, bool>();

                foreach (var url in urls)
                {
                    var uri = Ao3SiteDataLookup.ReadingListlUri(url);
                    if (!(uri is null)) urlmap.Add(new KeyValuePair<string, Uri>(url, uri));
                    result[url] = false;
                }

                foreach (var m in readingListBacking.AllSafe)
                {
                    for (var i = 0; i < urlmap.Count; i++)
                    {
                        var kvp = urlmap[i];
                        if (m.HasUri(kvp.Value))
                        {
                            result[kvp.Key] = true;
                            urlmap.RemoveAt(i);
                            i--;
                        }
                    }
                    if (urlmap.Count == 0)
                        break;
                }

                return result;
            });
        }

        public async Task<IDictionary<long, bool>> AreWorksInListAsync(long[] workids)
        {
            if (!restored.Task.IsCompleted)
                await restored.Task;

            return await Task.Run(() =>
            {
                var workmap = new List<KeyValuePair<long, Uri>>();
                IDictionary<long, bool> result = new Dictionary<long, bool>();

                foreach (var workid in workids)
                {
                    var uri = Ao3SiteDataLookup.ReadingListlUri("http://archiveofourown.org/works/" + workid);
                    if (!(uri is null)) workmap.Add(new KeyValuePair<long, Uri>(workid, uri));
                }

                foreach (var m in readingListBacking.AllSafe)
                {
                    for (var i = 0; i < workmap.Count; i++)
                    {
                        var kvp = workmap[i];
                        if (m.HasUri(kvp.Value))
                        {
                            result[kvp.Key] = true;
                            workmap.RemoveAt(i);
                            i--;
                        }
                    }
                    if (workmap.Count == 0)
                        break;
                }

                return result;
            });
        }

        private async Task WriteViewModelToDbAsync(Ao3PageViewModel viewmodel, ReadingList dbentry)
        {
            bool changed = false;
            if (dbentry.Unread != viewmodel.Unread)
            {
                changed = true;
                dbentry.Unread = viewmodel.Unread;
            }

            string model = Ao3PageModel.Serialize(viewmodel.BaseData);
            if (!(model is null) && dbentry.Model != model)
            {
                changed = true;
                dbentry.Model = model;
            }

            if (changed)
                await App.Database.ReadingListCached.InsertOrUpdateAsync(dbentry);
        }

        private async void Viewmodel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var viewmodel = (Ao3PageViewModel)sender;
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Unread" || e.PropertyName == "Summary")
            {
                var dbentry = await App.Database.ReadingListCached.SelectAsync(viewmodel.Uri.AbsoluteUri);
                if (!(dbentry is null)) await WriteViewModelToDbAsync(viewmodel, new ReadingList(dbentry)).ConfigureAwait(false);
            }
        }

        public async Task RefreshAsync(Ao3PageViewModel viewmodel)
        {
            var model = await Ao3SiteDataLookup.LookupAsync(viewmodel.Uri.AbsoluteUri);
            if (!(model is null))
            {
                if (viewmodel.Uri.AbsoluteUri != model.Uri.AbsoluteUri)
                {
                    await App.Database.ReadingListCached.DeleteAsync(viewmodel.Uri.AbsoluteUri);

                    var pvm = readingListBacking.FindInAll((m) => m.Uri.AbsoluteUri == model.Uri.AbsoluteUri);
                    if (!(pvm is null))
                    {
                        await wvp.DoOnMainThreadAsync(() =>
                        {
                            readingListBacking.Remove(pvm);
                            pvm.Dispose();
                        });
                    }
                }

                // If missing title in update, get it from old
                if (string.IsNullOrEmpty(model.Title)) model.Title = viewmodel.BaseData?.Title;

                await wvp.DoOnMainThreadAsync(async () =>
                {
                    await viewmodel.SetBaseDataAsync(model, true);
                });

                await WriteViewModelToDbAsync(viewmodel, new ReadingList(model, 0, viewmodel.Unread));
            }
        }
    }
}
