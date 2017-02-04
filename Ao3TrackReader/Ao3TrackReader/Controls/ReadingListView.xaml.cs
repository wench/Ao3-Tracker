using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using Ao3TrackReader.Resources;

namespace Ao3TrackReader.Controls
{
    public class RLMenuItem : MenuItem
    {
        public event Func<RLMenuItem, bool> Filter;

        public bool OnFilter()
        {
            return Filter?.Invoke(this) ?? true;
        }
    }

    public partial class ReadingListView : PaneView
    {
        GroupList<Models.Ao3PageViewModel> readingListBacking;
        private readonly WebViewPage wpv;
        SemaphoreSlim RefreshSemaphore = new SemaphoreSlim(10);

        public ReadingListView(WebViewPage wpv)
        {
            this.wpv = wpv;

            InitializeComponent();

            readingListBacking = new GroupList<Models.Ao3PageViewModel>();
            BackgroundColor = Colors.Alt.Trans.High;

            ShowHiddenButton.BackgroundColor = readingListBacking.ShowHidden ? Colors.Highlight.Trans.Medium : Color.Transparent;
            ShowTagsButton.BackgroundColor = TagsVisible ? Colors.Highlight.Trans.Medium : Color.Transparent;
            ListView.ItemAppearing += ListView_ItemAppearing;

            Device.BeginInvokeOnMainThread(() =>Task.Run(async () => { await RestoreReadingList(); }));            
        }

        async Task RestoreReadingList()
        { 
            // Restore the reading list contents!
            var items = new Dictionary<string, Models.ReadingList>();
            foreach (var i in App.Database.GetReadingListItems())
            {
                items[i.Uri] = i;
            }

            List<Task> tasks = new List<Task>();
            var vms = new List<Models.Ao3PageViewModel>();

            if (items.Count == 0)
            {
                tasks.Add(AddAsync("http://archiveofourown.org/"));
            }
            else
            {
                var timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000L;
                var models = Data.Ao3SiteDataLookup.LookupQuick(items.Keys);
                foreach (var model in models)
                {
                    if (model.Value != null)
                    {
                        var item = items[model.Key];
                        if (string.IsNullOrWhiteSpace(model.Value.Title) || model.Value.Type == Models.Ao3PageType.Work)
                            model.Value.Title = item.Title;
                        if (string.IsNullOrWhiteSpace(model.Value.PrimaryTag) || model.Value.PrimaryTag.StartsWith("<"))
                        {
                            model.Value.PrimaryTag = item.PrimaryTag;
                            var tagdata = Data.Ao3SiteDataLookup.LookupTagQuick(item.PrimaryTag);
                            if (tagdata != null) model.Value.PrimaryTagType = Data.Ao3SiteDataLookup.GetTypeForCategory(tagdata.category);
                            else model.Value.PrimaryTagType = Models.Ao3TagType.Other;
                        }
                        if (model.Value.Details != null && model.Value.Details.Summary == null && !string.IsNullOrEmpty(item.Summary))
                            model.Value.Details.Summary = item.Summary;

                        if (model.Value.Uri.AbsoluteUri != model.Key)
                        {
                            App.Database.DeleteReadingListItems(model.Key);
                            App.Database.SaveReadingListItems(new Models.ReadingList { Uri = model.Value.Uri.AbsoluteUri, PrimaryTag = model.Value.PrimaryTag, Title = model.Value.Title, Timestamp = timestamp, Unread = item.Unread });
                        }

                        if (readingListBacking.FindInAll((m) => m.Uri.AbsoluteUri == model.Value.Uri.AbsoluteUri) == null)
                        {
                            var viewmodel = new Models.Ao3PageViewModel(model.Value, item.Unread);
                            viewmodel.TagsVisible = tags_visible;
                            viewmodel.PropertyChanged += Viewmodel_PropertyChanged;
                            readingListBacking.Add(viewmodel);
                            vms.Add(viewmodel);
                        }
                    }
                }
            }
            SyncToServerAsync();

            wpv.DoOnMainThread(() =>
            {
                ListView.ItemsSource = readingListBacking;
            });

            foreach (var viewmodel in vms)
            {
                await RefreshSemaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    await RefreshAsync(viewmodel);
                    RefreshSemaphore.Release();
                }));

            }
            await Task.WhenAll(tasks);

            wpv.DoOnMainThread(() =>
            {
                RefreshButton.IsEnabled = true;
            });
        }

        private void ListView_ItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            var item = e.Item as Models.Ao3PageViewModel;
            if (item == null) return;
            Uri uri = Data.Ao3SiteDataLookup.ReadingListlUri(wpv.Current.AbsoluteUri);
            if (uri != null && item.HasUri(uri))
            {
                ListView.SelectedItem = null;
                ListView.SelectedItem = item;
            }
        }

        protected override void OnIsOnScreenChanging(bool newValue)
        {
            if (newValue == true)
            {
                ListView.Focus();
            }
            else
            {
                ListView.Unfocus();
            }
        }

        void GotoLast(Models.Ao3PageViewModel item)
        {
            if (item.BaseData.Type == Models.Ao3PageType.Work && item.BaseData.Details != null && item.BaseData.Details.WorkId != 0)
            {
                wpv.NavigateToLast(item.BaseData.Details.WorkId);
            }
            else if (item.BaseData.Type == Models.Ao3PageType.Series && item.BaseData.SeriesWorks?.Count > 0)
            {
                long workid = 0;

                foreach (var workmodel in item.BaseData.SeriesWorks)
                {
                    workid = workmodel.Details.WorkId;
                    int chapters_finished = 0;
                    if (item.WorkChapters.TryGetValue(workid, out var workchap))
                    {
                        chapters_finished = (int)workchap.number;
                        if (workchap.location != null) { chapters_finished--; }
                    }
                    if (chapters_finished < workmodel.Details.Chapters.Available) break;
                }

                if (workid != 0) wpv.NavigateToLast(workid);
                else wpv.Navigate(item.Uri);
            }
            else
            {
                wpv.Navigate(item.Uri);
            }
            IsOnScreen = false;
        }

        public void OnCellTapped(object sender, EventArgs e)
        {
            var mi = ((Cell)sender);
            var item = mi.BindingContext as Models.Ao3PageViewModel;
            if (item != null)
            {
                GotoLast(item);
            }
        }

        public void PageChange(Uri uri)
        {
            uri = Data.Ao3SiteDataLookup.ReadingListlUri(uri.AbsoluteUri);
            wpv.DoOnMainThread(() => {
                ListView.SelectedItem = null;
                if (uri == null) return;
                ListView.SelectedItem = readingListBacking.Find((m) => m.HasUri(uri));
            });
        }

        public void OnSelection(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
            {
                PageChange(wpv.Current);
                return;
            }
        }

        private bool MenuOpenLastFilter(RLMenuItem mi)
        {
            var item = mi.CommandParameter as Models.Ao3PageViewModel;
            if (item?.BaseData == null) return true;

            return item.BaseData.Type == Models.Ao3PageType.Work || item.BaseData.Type == Models.Ao3PageType.Series;
        }

        private void OnCellAppearing(object sender, EventArgs e)
        {
            var cell = ((Cell)sender);
            for (int i = 0; i < cell.ContextActions.Count; i++)
            {
                var mi = cell.ContextActions[i] as RLMenuItem;
                if (mi != null && mi.OnFilter() == false)
                {
                    cell.ContextActions.RemoveAt(i);
                    i--;
                }
            }
        }

        public void OnMenuOpenLast(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var item = mi.CommandParameter as Models.Ao3PageViewModel;
            if (item != null)
            {
                GotoLast(item);
            }
        }

        public void OnMenuOpen(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var item = mi.CommandParameter as Models.Ao3PageViewModel;
            if (item != null)
            {
                wpv.Navigate(item.Uri);
                IsOnScreen = false;
            }
        }

        public void OnMenuRefresh(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var item = mi.CommandParameter as Models.Ao3PageViewModel;
            if (item != null)
            {
                RefreshAsync(item);
            }

        }

        public void OnMenuDelete(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var item = mi.CommandParameter as Models.Ao3PageViewModel;
            if (item != null)
            {
                RemoveAsync(item.Uri.AbsoluteUri);
            }

        }

        public void OnAddPage(object sender, EventArgs e)
        {
            AddAsync(wpv.Current.AbsoluteUri);
        }

        public void OnRefresh(object sender, EventArgs e)
        {
            ListView.Focus();
            RefreshButton.IsEnabled = false;
            Task.Run(async () =>
            {
                List<Task> tasks = new List<Task>();
                foreach (var viewmodel in readingListBacking.AllSafe)
                {
                    await RefreshSemaphore.WaitAsync();

                    tasks.Add(Task.Run(async () => {
                        await RefreshAsync(viewmodel);
                        RefreshSemaphore.Release();
                    }));
                }


                await Task.WhenAll(tasks);

                wpv.DoOnMainThread(() =>
                {
                    RefreshButton.IsEnabled = true;
                    PageChange(wpv.Current);
                });
            });
        }

        public void OnShowHidden(object sender, EventArgs e)
        {
            readingListBacking.ShowHidden = !readingListBacking.ShowHidden;
            ShowHiddenButton.BackgroundColor = readingListBacking.ShowHidden ? Colors.Highlight.Trans.Medium : Color.Transparent;
        }

        public void OnShowTags(object sender, EventArgs e)
        {
            TagsVisible = !TagsVisible;
            ShowTagsButton.BackgroundColor = TagsVisible ? Colors.Highlight.Trans.Medium : Color.Transparent;
        }

        bool tags_visible = false;
        public bool TagsVisible
        {
            get { return tags_visible; }
            set
            {
                if (tags_visible != value)
                {
                    tags_visible = value;
                    readingListBacking.ForEachInAll((item) => item.TagsVisible = tags_visible);
                }
            }
        }


        public void OnClose(object sender, EventArgs e)
        {
            IsOnScreen = false;
        }

        public async void SyncToServerAsync()
        {
            var srl = new Models.ServerReadingList();
            try
            {
                srl.last_sync = Convert.ToInt64(App.Database.GetVariable("ReadingList.last_sync"));
            }
            catch
            {
                srl.last_sync = 0;
            }

            srl.paths = App.Database.GetReadingListItems().ToDictionary(i => i.Uri, i => i.Timestamp);

            srl = await App.Storage.SyncReadingListAsync(srl);
            if (srl != null)
            {
                List<Task> tasks = new List<Task>();
                foreach (var item in srl.paths)
                {
                    await RefreshSemaphore.WaitAsync();

                    tasks.Add(Task.Run(() => {
                        if (item.Value == -1)
                        {
                            RemoveAsyncImpl(item.Key);
                        }
                        else
                        {
                            AddAsyncImpl(item.Key, item.Value);
                        }
                        RefreshSemaphore.Release();
                    }));

                }
                await Task.WhenAll(tasks);
                App.Database.SaveVariable("ReadingList.last_sync", srl.last_sync.ToString());
            }
            PageChange(wpv.Current);
        }

        void RemoveAsyncImpl(string href)
        {
            App.Database.DeleteReadingListItems(href);
            var viewmodel = readingListBacking.FindInAll((m) => m.Uri.AbsoluteUri == href);
            if (viewmodel == null) return;
            viewmodel.PropertyChanged -= Viewmodel_PropertyChanged;
            wpv.DoOnMainThread(() =>
            {
                readingListBacking.Remove(viewmodel);
            });
        }

        public Task RemoveAsync(string href)
        {
            return Task.Run(() =>
            {
                RemoveAsyncImpl(href);
                SyncToServerAsync();
            });
        }

        void AddAsyncImpl(string href, long timestamp)
        {
            if (readingListBacking.FindInAll((m) => m.Uri.AbsoluteUri == href) != null)
                return;

            var models = Data.Ao3SiteDataLookup.LookupQuick(new[] { href });
            var model = models.SingleOrDefault();
            if (model.Value == null) return;

            if (readingListBacking.FindInAll((m) => m.Uri.AbsoluteUri == model.Value.Uri.AbsoluteUri) != null)
                return;

            App.Database.SaveReadingListItems(new Models.ReadingList { Uri = model.Value.Uri.AbsoluteUri, PrimaryTag = model.Value.PrimaryTag, Title = model.Value.Title, Timestamp = timestamp, Unread = null });
            wpv.DoOnMainThread(() =>
            {
                var viewmodel = new Models.Ao3PageViewModel(model.Value, null);
                viewmodel.TagsVisible = tags_visible;
                viewmodel.PropertyChanged += Viewmodel_PropertyChanged;
                readingListBacking.Add(viewmodel);
                Task.Run(async () =>
                {
                    await RefreshSemaphore.WaitAsync();
                    await RefreshAsync(viewmodel);
                    RefreshSemaphore.Release();
                });
            });
        }

        private void Viewmodel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var viewmodel = (Models.Ao3PageViewModel)sender;
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Unread" || e.PropertyName == "Summary")
            {
                bool changed = false;
                var dbentry = App.Database.GetReadingListItem(viewmodel.Uri.AbsoluteUri);
                if (dbentry != null)
                {
                    if (dbentry.Unread != viewmodel.Unread)
                    {
                        changed = true;
                        dbentry.Unread = viewmodel.Unread;
                    }

                    if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Summary")
                    {
                        string summary = viewmodel.Summary?.ToString();
                        if (summary != null && dbentry.Summary != summary)
                        {
                            changed = true;
                            dbentry.Summary = summary;
                        }
                    }

                    if (changed) App.Database.SaveReadingListItems(dbentry);
                }
            }
        }

        public Task AddAsync(string href)
        {
            return Task.Run(() =>
            {
                AddAsyncImpl(href, (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000L);
                SyncToServerAsync();
            });
        }

        public Task RefreshAsync(Models.Ao3PageViewModel viewmodel)
        {
            return Task.Run(async () =>
            {
                var models = await Data.Ao3SiteDataLookup.LookupAsync(new[] { viewmodel.Uri.AbsoluteUri });

                var model = models.SingleOrDefault();
                if (model.Value != null)
                {
                    if (viewmodel.Uri.AbsoluteUri != model.Value.Uri.AbsoluteUri)
                    {
                        App.Database.DeleteReadingListItems(viewmodel.Uri.AbsoluteUri);

                        var pvm = readingListBacking.FindInAll((m) => m.Uri.AbsoluteUri == model.Value.Uri.AbsoluteUri);
                        if (pvm != null) readingListBacking.Remove(pvm);
                    }
                    App.Database.SaveReadingListItems(new Models.ReadingList { Uri = model.Value.Uri.AbsoluteUri, PrimaryTag = model.Value.PrimaryTag, Title = model.Value.Title, Unread = viewmodel.Unread });
                }
                wpv.DoOnMainThread(() =>
                {
                    viewmodel.BaseData = model.Value;
                    //if (readingListBacking.FindInAll((m) => m.Uri.AbsoluteUri == viewmodel.Uri.AbsoluteUri) == null)
                    //    readingListBacking.Add(viewmodel);
                });
            });
        }
    }
}
