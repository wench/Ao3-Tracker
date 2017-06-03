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

#if __ANDROID__
// Android doesn't support: Enough ES2015, passing js functions directly to native code
// Android supports: Native methods returning values to js
#define NEED_INJECT_POLYFILLS
#define NEED_INJECT_CALLBACKS

#elif __IOS__
// iOS doesn't support: Enough ES2015, native methods returning values to js, passing js functions directly to native
#define NEED_INJECT_POLYFILLS
#define NEED_INJECT_CALLBACKS
#define NEED_INJECT_MESSAGING

#elif WINDOWS_UWP
// UWP supports: Enough ES2015, native methods returning values to js, passing js functions directly to native

#elif __WINDOWS__
// Win8.1 doesn't support: Enough ES2015, native methods returning values to js, passing js functions directly to native
#define NEED_INJECT_POLYFILLS
#define NEED_INJECT_CALLBACKS
#define NEED_INJECT_MESSAGING

#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Ao3TrackReader.Helper;
using Ao3TrackReader.Data;
using System.Threading;
using System.Text.RegularExpressions;
using Ao3TrackReader.Controls;
using Ao3TrackReader.Resources;

using Xamarin.Forms.PlatformConfiguration;
using System.Runtime.CompilerServices;

#if WINDOWS_UWP
using Xamarin.Forms.PlatformConfiguration.WindowsSpecific;
#elif __ANDROID__
using Android.OS;
#endif

using ToolbarItem = Ao3TrackReader.Controls.ToolbarItem;
using OperationCanceledException = System.OperationCanceledException;

namespace Ao3TrackReader
{
    public sealed partial class WebViewPage : ContentPage, IWebViewPage, IPageEx, IWebViewPageNative
    {
        IAo3TrackHelper helper;

        public string[] Injections { get; } =
            new[] {
                "jquery-3.1.1.js",
                "init.js",
                "marshal.js",
                "tracker.css",
#if NEED_INJECT_POLYFILLS
                "polyfills.js",
#endif
                "utils.js",
#if NEED_INJECT_CALLBACKS
                "callbacks.js",
#endif
                "platform.js",
#if NEED_INJECT_MESSAGING
                "messaging.js",
#endif
                "reader.js",
                "tracker.js",
                "unitconv.js",
                "touch.js"
            };

        public IEnumerable<Models.IHelpInfo> HelpItems
        {
            get { return ExtraHelp.Concat((IEnumerable<Models.IHelpInfo>)AllToolbarItems); }
        }

        public ReadingListView ReadingList { get { return ReadingListPane; } }

        public WebViewPage()
        {
            InitializeToolbarCommands();

            TitleEx = "Loading...";

            App.Current.HaveNetworkChanged += App_HaveNetworkChanged;

            InitializeComponent();

            UpdateBackButton();

            foreach (var tbi in AllToolbarItems)
            {
                tbi.PropertyChanged += ToolBarItem_PropertyChanged;
            }

            HelpPane.Init();
            SetupContextMenu();
            UpdateToolbar();

            InitSettings();

            WebViewHolder.Content = CreateWebView();

            ListFiltering.Create();

            string url = App.Database.GetVariable("Sleep:URI");
            App.Database.DeleteVariable("Sleep:URI");

            Uri uri = null;
            if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = Data.Ao3SiteDataLookup.CheckUri(uri);

            if (uri == null) uri = new Uri("http://archiveofourown.org/");

            // restore font size!
            if (App.Database.TryGetVariable("LogFontSize", int.TryParse, out int lfs)) LogFontSize = lfs;
            else LogFontSize = 0;

            App.Database.TryGetVariable("ToolbarBackBehaviour", Enum.TryParse<NavigateBehaviour>, out ToolbarBackBehaviour, NavigateBehaviour.History);
            App.Database.TryGetVariable("ToolbarForwardBehaviour", Enum.TryParse<NavigateBehaviour>, out ToolbarForwardBehaviour, NavigateBehaviour.HistoryThenPage);
            App.Database.TryGetVariable("SwipeBackBehaviour", Enum.TryParse<NavigateBehaviour>, out SwipeBackBehaviour, NavigateBehaviour.History);
            App.Database.TryGetVariable("SwipeForwardBehaviour", Enum.TryParse<NavigateBehaviour>, out SwipeForwardBehaviour, NavigateBehaviour.PageThenHistory);

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                Navigate(uri);
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    App_HaveNetworkChanged(App.Current, App.Current.HaveNetwork);
                });
            });
        }

        private void App_HaveNetworkChanged(object sender, EventArgs<bool> e)
        {
            if (!e) ShowError("No Internet Connection");
        }

        private void UrlBar_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "IsVisible")
            {
                if (urlBar.IsVisible == false) UrlBarToolBarItem.Foreground = Xamarin.Forms.Color.Default;
                else UrlBarToolBarItem.Foreground = Colors.Highlight.High;
            }
        }

        private void ReadingList_IsOnScreenChanged(object sender, EventArgs<bool> onscreen)
        {
            if (!onscreen.Value) ReadingListToolBarItem.Foreground = Xamarin.Forms.Color.Default;
            else ReadingListToolBarItem.Foreground = Colors.Highlight.High;
        }

        private void SettingsPane_IsOnScreenChanged(object sender, EventArgs<bool> onscreen)
        {
            if (!onscreen.Value) SettingsToolBarItem.Foreground = Xamarin.Forms.Color.Default;
            else SettingsToolBarItem.Foreground = Colors.Highlight.High;
        }

        private void HelpPane_IsOnScreenChanged(object sender, EventArgs<bool> onscreen)
        {
            if (!onscreen.Value) HelpToolBarItem.Foreground = Xamarin.Forms.Color.Default;
            else HelpToolBarItem.Foreground = Colors.Highlight.High;
        }

        private void TogglePane(PaneView pane)
        {
            foreach (var c in Panes.Children)
            {
                if (c != pane) c.IsOnScreen = false;
            }
            if (pane != null) pane.IsOnScreen = !pane.IsOnScreen;
        }

        public DisableableCommand JumpCommand { get; private set; }
        public DisableableCommand FontIncreaseCommand { get; private set; }
        public DisableableCommand FontDecreaseCommand { get; private set; }
        public DisableableCommand ForwardCommand { get; private set; }
        public DisableableCommand BackCommand { get; private set; }
        public DisableableCommand SyncCommand { get; private set; }
        public DisableableCommand ForceSetLocationCommand { get; private set; }
        public DisableableCommand RefreshCommand { get; private set; }
        public DisableableCommand ReadingListCommand { get; private set; }
        public DisableableCommand UrlBarCommand { get; private set; }
        public DisableableCommand ResetFontSizeCommand { get; private set; }
        public DisableableCommand SettingsCommand { get; private set; }
        public DisableableCommand HelpCommand { get; private set; }

        void InitializeToolbarCommands()
        {
            BackCommand = new DisableableCommand(ToolbarGoBack, false);
            ForwardCommand = new DisableableCommand(ToolbarGoForward, false);
            JumpCommand = new DisableableCommand(OnJumpClicked, false);
            FontIncreaseCommand = new DisableableCommand(() => LogFontSize++);
            FontDecreaseCommand = new DisableableCommand(() => LogFontSize--);
            ForceSetLocationCommand = new DisableableCommand(ForceSetLocation);

            SyncCommand = new DisableableCommand(() => App.Storage.DoSyncAsync(true), !App.Storage.IsSyncing && App.Storage.CanSync);
            App.Storage.BeginSyncEvent += (sender, e) => DoOnMainThreadAsync(() => { SyncCommand.IsEnabled = false; }).ConfigureAwait(false);
            App.Storage.EndSyncEvent += (sender, e) => DoOnMainThreadAsync(() => { SyncCommand.IsEnabled = !App.Storage.IsSyncing && App.Storage.CanSync; }).ConfigureAwait(false);
            SyncCommand.IsEnabled = !App.Storage.IsSyncing && App.Storage.CanSync;

            RefreshCommand = new DisableableCommand(Refresh);
            ReadingListCommand = new DisableableCommand(() => TogglePane(ReadingList));

            UrlBarCommand = new DisableableCommand(() =>
            {
                if (urlBar.IsVisible)
                {
                    urlBar.IsVisible = false;
                    urlBar.Unfocus();
                }
                else
                {
                    UpdateUrlBar(CurrentUri);
                    urlBar.IsVisible = true;
                    urlEntry.Focus();
                }
            });

            ResetFontSizeCommand = new DisableableCommand(() => LogFontSize = 0);
            SettingsCommand = new DisableableCommand(() => TogglePane(SettingsPane));
            HelpCommand = new DisableableCommand(() => TogglePane(HelpPane));
        }

        public void UpdateBackButton()
        {
            var mode = App.InteractionMode;

            bool? show = null;
            if (mode == InteractionMode.Phone || mode == InteractionMode.Tablet)
            {
                App.Database.TryGetVariable("ShowBackButton", bool.TryParse, out show);
            }

            bool def;
            if (mode == InteractionMode.Phone) def = false;
            else if (mode == InteractionMode.Tablet) def = true;
            else def = true;

            BackToolBarItem.IsVisible = show ?? def;
        }

        private void ToolBarItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "IsVisible")
            {
                UpdateToolbar();
            }
        }

        void UpdateToolbar()
        {
            int i = 0;
            foreach (var tbi in AllToolbarItems)
            {
                var c = i < ToolbarItems.Count ? ToolbarItems[i] : null;

                if (tbi.IsVisible)
                {
                    if (tbi != c)
                    {
                        ToolbarItems.Insert(i, tbi);
                    }
                    i++;
                }
                else if (tbi == c)
                {
                    ToolbarItems.RemoveAt(i);
                }
            }
        }

        List<KeyValuePair<string, DisableableCommand<string>>> ContextMenuItems { get; set; }
        DisableableCommand<string> ContextMenuOpenAdd;
        DisableableCommand<string> ContextMenuAdd;
        DisableableCommand<string> ContextMenuRemove;
        DisableableCommand<string> ContextMenuAddFilter;
        DisableableCommand<string> ContextMenuRemoveFilter;
        string ContextMenuFilterDetails;

        void SetupContextMenu()
        {
            ContextMenuItems = new List<KeyValuePair<string, DisableableCommand<string>>>
            {
                new KeyValuePair<string, DisableableCommand<string>>("Open", new DisableableCommand<string>((url) =>
                {
                    Navigate(new Uri(url));
                })),

                new KeyValuePair<string, DisableableCommand<string>>("Open and Add", ContextMenuOpenAdd = new DisableableCommand<string>((url) =>
                {
                    AddToReadingList(url);
                    Navigate(new Uri(url));
                })),

                new KeyValuePair<string, DisableableCommand<string>>("Add to Reading list", ContextMenuAdd = new DisableableCommand<string>((url) =>
                {
                    AddToReadingList(url);
                })),

                new KeyValuePair<string, DisableableCommand<string>>("Remove from Reading list", ContextMenuRemove = new DisableableCommand<string>((url) =>
                {
                    RemoveFromReadingList(url);
                })),

                new KeyValuePair<string, DisableableCommand<string>>("Add as Listing Filter", ContextMenuAddFilter = new DisableableCommand<string>((url) =>
                {
                    Data.ListFiltering.Instance.AddFilterAsync(ContextMenuFilterDetails);
                })),

                new KeyValuePair<string, DisableableCommand<string>>("Remove as Listing Filter", ContextMenuRemoveFilter = new DisableableCommand<string>((url) =>
                {
                    Data.ListFiltering.Instance.RemoveFilterAsync(ContextMenuFilterDetails);
                })),

                new KeyValuePair<string, DisableableCommand<string>>("Copy Link", new DisableableCommand<string>((url) =>
                {
                    CopyToClipboard(url, "url");
                }) { IsEnabled = HaveClipboard })
            };
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            UpdateBackButton();
        }

        public void OnSleep()
        {
            var loc = CurrentLocation;
            var uri = CurrentUri;
            if (loc != null)
            {
                uri = new Uri(uri, "#ao3tjump:" + loc.number.ToString() + ":" + loc.chapterid.ToString() + (loc.location == null ? "" : (":" + loc.location.ToString())));
            }
            App.Database.SaveVariable("Sleep:URI", uri.AbsoluteUri);
        }

        public void OnResume()
        {
            App.Database.DeleteVariable("Sleep:URI");
        }

        public async void NavigateToLast(long workid, bool fullwork)
        {
            var workchaps = await App.Storage.GetWorkChaptersAsync(new[] { workid }).ConfigureAwait(false);

            await DoOnMainThreadAsync(() =>
            {
                UriBuilder uri = new UriBuilder("http://archiveofourown.org/works/" + workid.ToString());
                if (!fullwork && workchaps.TryGetValue(workid, out var wc) && wc.chapterid != 0)
                {
                    uri.Path = uri.Path += "/chapters/" + wc.chapterid;
                }
                if (fullwork) uri.Query = "view_full_work=true";
                uri.Fragment = "ao3tjump";
                Navigate(uri.Uri);
            });
        }

        public void Navigate(long workid, bool fullwork)
        {
            DoOnMainThreadAsync(() =>
            {
                UriBuilder uri = new UriBuilder("http://archiveofourown.org/works/" + workid.ToString());
                if (fullwork) uri.Query = "view_full_work=true";
                uri.Fragment = "ao3tjump";
                Navigate(uri.Uri);
            }).ConfigureAwait(false);
        }

        private void UpdateUrlBar(Uri uri)
        {
            if (urlEntry != null)
            {
                if (uri.Fragment?.StartsWith("#ao3tjump") == true)
                    urlEntry.Text = uri.PathAndQuery;
                else
                    urlEntry.Text = uri.PathAndQuery + uri.Fragment;
            }
        }

        private void UrlCancel_Clicked(object sender, EventArgs e)
        {
            urlBar.IsVisible = false;
            urlBar.Unfocus();
        }

        private async void UrlButton_Clicked(object sender, EventArgs e)
        {
            urlBar.IsVisible = false;
            urlBar.Unfocus();
            try
            {
                var uri = Ao3SiteDataLookup.CheckUri(new Uri(new Uri("http://archiveofourown.org/"), urlEntry.Text));
                if (uri != null)
                {
                    Navigate(uri);
                }
                else
                {
                    await DisplayAlert("Url error", "Can only enter valid urls on archiveofourown.org", "Ok");
                }
            }
            catch
            {

            }

        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == "Title")
            {
                TitleEx = Title;
            }
        }

        public Text.TextEx TitleEx
        {
            get
            {
                return PageEx.GetTitleEx(this);
            }
            set
            {
                PageEx.SetTitleEx(this, value);
            }
        }

        PageTitle pageTitle = null;
        public PageTitle PageTitle
        {
            get { return pageTitle; }
            set
            {
                pageTitle = value;

                // Title by Author,Author - Chapter N: Title - Relationship - Fandoms

                var ts = new Text.Span();

                ts.Nodes.Add(pageTitle.Title);

                if (pageTitle.Authors != null && pageTitle.Authors.Length != 0)
                {
                    ts.Nodes.Add(new Text.String { Text = " by ", Foreground = Colors.Base });

                    bool first = true;
                    foreach (var user in pageTitle.Authors)
                    {
                        if (!first)
                            ts.Nodes.Add(new Text.String { Text = ", ", Foreground = Colors.Base });
                        else
                            first = false;

                        ts.Nodes.Add(user.Replace(' ', '\xA0'));
                    }
                }

                if (!string.IsNullOrWhiteSpace(pageTitle.Chapter) || !string.IsNullOrWhiteSpace(pageTitle.Chaptername))
                {
                    ts.Nodes.Add(new Text.String { Text = " | ", Foreground = Colors.Base });

                    if (!string.IsNullOrWhiteSpace(pageTitle.Chapter))
                    {
                        ts.Nodes.Add(pageTitle.Chapter.Replace(' ', '\xA0'));

                        if (!string.IsNullOrWhiteSpace(pageTitle.Chaptername))
                            ts.Nodes.Add(new Text.String { Text = ": ", Foreground = Colors.Base });
                    }
                    if (!string.IsNullOrWhiteSpace(pageTitle.Chaptername))
                        ts.Nodes.Add(pageTitle.Chaptername.Replace(' ', '\xA0'));
                }

                if (!string.IsNullOrWhiteSpace(pageTitle.Primarytag))
                {
                    ts.Nodes.Add(new Text.String { Text = " | ", Foreground = Colors.Base });
                    ts.Nodes.Add(pageTitle.Primarytag.Replace(' ', '\xA0'));
                }

                if (pageTitle.Fandoms != null && pageTitle.Fandoms.Length != 0)
                {
                    ts.Nodes.Add(new Text.String { Text = " | ", Foreground = Colors.Base });

                    bool first = true;
                    foreach (var fandom in pageTitle.Fandoms)
                    {
                        if (!first)
                            ts.Nodes.Add(new Text.String { Text = ", ", Foreground = Colors.Base });
                        else
                            first = false;

                        ts.Nodes.Add(fandom.Replace(' ', '\xA0'));
                        if (Xamarin.Forms.Device.Idiom == TargetIdiom.Phone)
                            break;
                    }
                }

                TitleEx = ts;
            }
        }

        public int LogFontSizeMax { get { return 30; } }
        public int LogFontSizeMin { get { return -30; } }
        private int log_font_size = 0;

        public int LogFontSize
        {
            get { return log_font_size; }

            set
            {
                if (log_font_size != value)
                {
                    Task.Run(() =>
                    {
                        App.Database.SaveVariable("LogFontSize", value);
                    });
                }

                log_font_size = value;
                if (log_font_size < LogFontSizeMin) log_font_size = LogFontSizeMin;
                if (log_font_size > LogFontSizeMax) log_font_size = LogFontSizeMax;

                DoOnMainThreadAsync(() =>
                {
                    ResetFontSizeCommand.IsEnabled = log_font_size != 0;
                    FontDecreaseCommand.IsEnabled = log_font_size > LogFontSizeMin;
                    FontIncreaseCommand.IsEnabled = log_font_size < LogFontSizeMax;
                    helper?.OnAlterFontSize(FontSize);
                }).ConfigureAwait(false);
            }
        }
        public int FontSize
        {
            get { return (int)Math.Round(100.0 * Math.Pow(1.05, LogFontSize), MidpointRounding.AwayFromZero); }
        }


        static object locker = new object();

        public async Task<IDictionary<long, IWorkDetails>> GetWorkDetailsAsync(long[] works, WorkDetailsFlags flags)
        {
            var result = new Dictionary<long, IWorkDetails>();
            var tasks = new List<Task>();

            if ((flags & WorkDetailsFlags.SavedLoc) != 0)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var workchapters = await App.Storage.GetWorkChaptersAsync(works);
                    lock (result)
                    {
                        foreach (var kvp in workchapters)
                        {
                            if (!result.TryGetValue(kvp.Key, out var detail))
                                result.Add(kvp.Key, detail = new WorkDetails());
                            detail.savedLoc = kvp.Value;
                        }
                    }
                }));
            }

            if ((flags & WorkDetailsFlags.InReadingList) != 0)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var areinrl = await ReadingList.AreWorksInListAsync(works);
                    lock (result)
                    {
                        foreach (var kvp in areinrl)
                        {
                            if (!result.TryGetValue(kvp.Key, out var detail))
                                result.Add(kvp.Key, detail = new WorkDetails());
                            detail.inReadingList = kvp.Value;
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            return result;
        }

        public Task<IDictionary<string, bool>> AreUrlsInReadingListAsync(string[] urls)
        {
            return ReadingList.AreUrlsInListAsync(urls);
        }

        public Task<T> DoOnMainThreadAsync<T>(Func<T> function)
        {
            if (IsMainThread)
            {
                return Task.FromResult(function());
            }
            else
            {
                var cs = new TaskCompletionSource<T>();

                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    cs.SetResult(function());
                });
                return cs.Task;
            }
        }

        public Task DoOnMainThreadAsync(MainThreadAction function)
        {
            if (IsMainThread)
            {
                function();

#if WINDOWS_APP || WINDOWS_PHONE_APP
                var complete = new TaskCompletionSource<object>();
                complete.SetResult(null);
                return complete.Task;
#else
                return Task.CompletedTask;
#endif
            }
            else
            {
                var complete = new TaskCompletionSource<object>();
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    function();
                    complete.SetResult(null);
                });
                return complete.Task;
            }
        }

        public Task DoOnMainThreadAsync(Func<Task> function)
        {
            if (IsMainThread)
            {
                return function();
            }
            else
            {
                var complete = new TaskCompletionSource<object>();
                Xamarin.Forms.Device.BeginInvokeOnMainThread(async () =>
                {
                    await function();
                    complete.SetResult(null);
                });
                return complete.Task;
            }
        }

        public Task<T> DoOnMainThreadAsync<T>(Func<Task<T>> function)
        {
            if (IsMainThread)
            {
                return function();
            }
            else
            {
                var complete = new TaskCompletionSource<T>();
                Xamarin.Forms.Device.BeginInvokeOnMainThread(async () =>
                {
                    complete.SetResult(await function());
                });
                return complete.Task;
            }
        }

        public async void SetWorkChaptersAsync(IDictionary<long, WorkChapter> works)
        {
            await App.Storage.SetWorkChaptersAsync(works);

            lock (currentLocationLock)
            {
                if (currentSavedLocation != null && currentLocation != null && works.TryGetValue(currentLocation.workid, out var workchap))
                {
                    workchap.workid = currentLocation.workid;
                    UpdateCurrentSavedLocation(workchap);
                }
            }
        }

        public void OnJumpClicked()
        {
            helper.OnJumpToLastLocation(true);
        }

        bool jumpToLastLocationSetup = false;
        public bool JumpToLastLocationEnabled
        {
            set
            {
                jumpToLastLocationSetup = value;
                if (JumpCommand != null) JumpCommand.IsEnabled = jumpToLastLocationSetup && ForceSetLocationCommand.IsEnabled;
            }
            get
            {
                return jumpToLastLocationSetup;
            }
        }

        public int ShowPrevPageIndicator
        {
            get
            {
                if (PrevPageIndicator.TextColor == Colors.Highlight)
                    return 2;
                else if (PrevPageIndicator.TextColor == Colors.Base.VeryLow)
                    return 1;
                return 0;
            }
            set
            {
                if (value == 0)
                    PrevPageIndicator.TextColor = BackgroundColor;
                else if (value == 1)
                    PrevPageIndicator.TextColor = Colors.Base.VeryLow;
                else if (value == 2)
                    PrevPageIndicator.TextColor = Colors.Highlight;
            }
        }
        public int ShowNextPageIndicator
        {
            get
            {
                if (NextPageIndicator.TextColor == Colors.Highlight)
                    return 2;
                else if (NextPageIndicator.TextColor == Colors.Base.VeryLow)
                    return 1;
                return 0;
            }
            set
            {
                if (value == 0)
                    NextPageIndicator.TextColor = BackgroundColor;
                else if (value == 1)
                    NextPageIndicator.TextColor = Colors.Base.VeryLow;
                else if (value == 2)
                    NextPageIndicator.TextColor = Colors.Highlight;
            }
        }

        private Uri nextPage;
        public string NextPage
        {
            get
            {
                return nextPage?.AbsoluteUri;
            }
            set
            {
                nextPage = null;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        nextPage = new Uri(CurrentUri, value);
                    }
                    catch
                    {

                    }
                }
                ForwardCommand.IsEnabled = ToolbarCanGoForward;
            }
        }
        private Uri prevPage;
        public string PrevPage
        {
            get
            {
                return prevPage?.AbsoluteUri;
            }
            set
            {
                prevPage = null;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        prevPage = new Uri(CurrentUri, value);
                    }
                    catch
                    {
                    }
                }
                BackCommand.IsEnabled = ToolbarCanGoBack;
            }
        }


        bool CanGoBack(NavigateBehaviour behaviour)
        {
            return (WebViewCanGoBack && behaviour.HasFlag(NavigateBehaviour.History)) ||
                (prevPage != null && behaviour.HasFlag(NavigateBehaviour.Page));
        }

        void GoBack(NavigateBehaviour behaviour)
        {
            bool h1 = false;
            bool p = false;
            bool h2 = false;

            if (behaviour.HasFlag(NavigateBehaviour.HistoryFirst))
            {
                h1 = behaviour.HasFlag(NavigateBehaviour.History); ;
                p = behaviour.HasFlag(NavigateBehaviour.Page);
            }
            else
            {
                p = behaviour.HasFlag(NavigateBehaviour.Page);
                h2 = behaviour.HasFlag(NavigateBehaviour.History);
            }

            if (h1 && WebViewCanGoBack) WebViewGoBack();
            else if (p && prevPage != null) Navigate(prevPage);
            else if (h2 && WebViewCanGoBack) WebViewGoBack();
        }

        bool CanGoForward(NavigateBehaviour behaviour)
        {
            return (WebViewCanGoForward && behaviour.HasFlag(NavigateBehaviour.History)) ||
                (nextPage != null && behaviour.HasFlag(NavigateBehaviour.Page));
        }

        void GoForward(NavigateBehaviour behaviour)
        {
            bool h1 = false;
            bool p = false;
            bool h2 = false;

            if (behaviour.HasFlag(NavigateBehaviour.HistoryFirst))
            {
                h1 = behaviour.HasFlag(NavigateBehaviour.History);
                p = behaviour.HasFlag(NavigateBehaviour.Page);
            }
            else
            {
                p = behaviour.HasFlag(NavigateBehaviour.Page);
                h2 = behaviour.HasFlag(NavigateBehaviour.History);
            }

            if (h1 && WebViewCanGoForward) WebViewGoForward();
            else if (p && nextPage != null) Navigate(nextPage);
            else if (h2 && WebViewCanGoForward) WebViewGoForward();
        }

        public NavigateBehaviour ToolbarBackBehaviour = NavigateBehaviour.History;
        public bool ToolbarCanGoBack => CanGoBack(ToolbarBackBehaviour);
        public void ToolbarGoBack()
        {
            GoBack(ToolbarBackBehaviour);
        }

        public NavigateBehaviour ToolbarForwardBehaviour = NavigateBehaviour.HistoryThenPage;
        public bool ToolbarCanGoForward => CanGoForward(ToolbarForwardBehaviour);
        public void ToolbarGoForward()
        {
            GoForward(ToolbarForwardBehaviour);
        }

        public NavigateBehaviour SwipeBackBehaviour = NavigateBehaviour.History;
        public bool SwipeCanGoBack => CanGoBack(SwipeBackBehaviour);
        public void SwipeGoBack()
        {
            GoBack(SwipeBackBehaviour);
        }

        public NavigateBehaviour SwipeForwardBehaviour = NavigateBehaviour.PageThenHistory;
        public bool SwipeCanGoForward => CanGoForward(SwipeForwardBehaviour);
        public void SwipeGoForward()
        {
            GoForward(SwipeForwardBehaviour);
        }

        public void AddToReadingList(string href)
        {
            ReadingList.AddAsync(href);
        }

        public void RemoveFromReadingList(string href)
        {
            ReadingList.RemoveAsync(href);
        }

        public void SetCookies(string cookies)
        {
            if (App.Database.GetVariable("siteCookies") != cookies)
                App.Database.SaveVariable("siteCookies", cookies);
        }

        protected override bool OnBackButtonPressed()
        {
            foreach (var p in Panes.Children)
            {
                if (p.IsOnScreen)
                {
                    p.IsOnScreen = false;
                    return true;
                }
            }
            if (SwipeCanGoBack)
            {
                SwipeGoBack();
                return true;
            }
            return false;
        }

        IWorkChapterEx currentLocation;
        WorkChapter currentSavedLocation;
        object currentLocationLock = new object();
        public IWorkChapterEx CurrentLocation
        {
            get { return currentLocation; }
            set
            {
                lock (currentLocationLock)
                {
                    currentLocation = value;
                    if (currentLocation != null && currentLocation.workid == currentSavedLocation?.workid)
                    {
                        ForceSetLocationCommand.IsEnabled = currentLocation.LessThan(currentSavedLocation);
                        JumpCommand.IsEnabled = jumpToLastLocationSetup && ForceSetLocationCommand.IsEnabled;
                        return;
                    }
                    else if (currentLocation == null)
                    {
                        ForceSetLocationCommand.IsEnabled = false;
                        JumpCommand.IsEnabled = jumpToLastLocationSetup && ForceSetLocationCommand.IsEnabled;
                        return;
                    }

                }
                Task.Run(async () =>
                {
                    long workid = 0;
                    lock (currentLocationLock)
                    {
                        if (currentLocation != null) workid = currentLocation.workid;
                    }
                    var workchap = workid == 0 ? null : (await App.Storage.GetWorkChaptersAsync(new[] { workid })).Select(kvp => kvp.Value).FirstOrDefault();
                    if (workchap != null) workchap.workid = workid;
                    UpdateCurrentSavedLocation(workchap);
                });
            }
        }

        void UpdateCurrentSavedLocation(WorkChapter workchap)
        {
            lock (currentLocationLock)
            {
                if (currentSavedLocation == null || currentSavedLocation.LessThan(workchap))
                {
                    currentSavedLocation = workchap;
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                    {
                        lock (currentLocationLock)
                        {
                            if (currentLocation != null && currentLocation.workid == currentSavedLocation?.workid)
                                ForceSetLocationCommand.IsEnabled = currentLocation.LessThan(currentSavedLocation);
                            else
                                ForceSetLocationCommand.IsEnabled = false;

                            JumpCommand.IsEnabled = jumpToLastLocationSetup && ForceSetLocationCommand.IsEnabled;
                        }
                    });
                }
            }
        }


        public async void ForceSetLocation()
        {
            if (currentLocation != null)
            {
                var db_workchap = (await App.Storage.GetWorkChaptersAsync(new[] { currentLocation.workid })).Select((kp) => kp.Value).FirstOrDefault();
                currentLocation = currentSavedLocation = new WorkChapter(currentLocation) { seq = (db_workchap?.seq ?? 0) + 1 };
                await App.Storage.SetWorkChaptersAsync(new Dictionary<long, WorkChapter> { [currentLocation.workid] = currentSavedLocation }).ConfigureAwait(false);
            }
        }


        System.Diagnostics.Stopwatch webViewDragAccelerateStopWatch = null;
        public void StopWebViewDragAccelerate()
        {
            if (webViewDragAccelerateStopWatch != null)
                webViewDragAccelerateStopWatch.Reset();
        }

        int SwipeOffsetChanged(double offset)
        {
            var end = DeviceWidth;
            var centre = end / 2;
            if ((!SwipeCanGoBack && offset > 0.0) || (!SwipeCanGoForward && offset < 0.0))
            {
                offset = 0.0;
            }
            else if (offset < -end)
            {
                offset = -end;
            }
            else if (offset > end)
            {
                offset = end;
            }
            LeftOffset = offset;


            if (SwipeCanGoForward && offset < -centre)
            {
                ShowNextPageIndicator = 2;
                if (offset <= -end) return -3;
                return -2;
            }
            else if (SwipeCanGoForward && offset < 0)
            {
                ShowNextPageIndicator = 1;
            }
            else
            {
                ShowNextPageIndicator = 0;
            }

            if (SwipeCanGoBack && offset >= centre)
            {
                ShowPrevPageIndicator = 2;
                if (offset >= end) return 3;
                return 2;
            }
            else if (SwipeCanGoBack && offset > 0)
            {
                ShowPrevPageIndicator = 1;
            }
            else
            {
                ShowPrevPageIndicator = 0;
            }

            if (offset == 0) return 0;
            else if (offset < 0) return -1;
            else return 1;
        }

        public void StartWebViewDragAccelerate(double velocity)
        {
            if (webViewDragAccelerateStopWatch == null) webViewDragAccelerateStopWatch = new System.Diagnostics.Stopwatch();
            webViewDragAccelerateStopWatch.Restart();
            double lastTime = 0;
            double offset = LeftOffset;
            var offsetCat = SwipeOffsetChanged(offset);

            Device.StartTimer(TimeSpan.FromMilliseconds(15),
                () =>
                {
                    if (!webViewDragAccelerateStopWatch.IsRunning) return false;

                    double now = webViewDragAccelerateStopWatch.ElapsedMilliseconds;

                    double acceleration = 0;   // pixels/s^2
                    if (offsetCat <= -2) acceleration = -3000.0;
                    else if (offsetCat == -1) acceleration = 3000.0;
                    else if (offsetCat >= 2) acceleration = 3000.0;
                    else if (offsetCat == 1) acceleration = -3000.0;
                    else
                    {
                        return false;
                    }

                    var oldoffset = offset;
                    velocity = velocity + acceleration * (now - lastTime) / 1000.0;
                    offset = offset + velocity * (now - lastTime) / 1000.0;

                    if ((oldoffset < 0 && offset >= 0) || (oldoffset > 0 && offset <= 0))
                    {
                        LeftOffset = 0;
                        return false;
                    }

                    offsetCat = SwipeOffsetChanged(offset);
                    if (offsetCat == 3)
                    {
                        SwipeGoBack();
                        return false;
                    }
                    else if (offsetCat == -3)
                    {
                        SwipeGoForward();
                        return false;
                    }

                    lastTime = now;
                    return true;
                });
        }

        public async Task<string> CallJavascriptAsync(string function, params object[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == null)
                {
                    args[i] = "null";
                    continue;
                }
                var type = args[i].GetType();
                args[i] = Newtonsoft.Json.JsonConvert.SerializeObject(args[i]);
            }
            try
            {
                return await EvaluateJavascriptAsync(function + "(" + string.Join(",", args) + ");");
            }
            catch
            {
                return null;
            }
        }

        private void OnWebViewGotFocus()
        {
            TogglePane(null);
        }

        private CancellationTokenSource cancelInject;
        private bool OnNavigationStarting(Uri uri)
        {
            if (uri != null)
            {
                var check = Ao3SiteDataLookup.CheckUri(uri);
                if (check == null)
                {
                    // Handle external uri
                    LeftOffset = 0;
                    OpenExternal(uri);
                    return true;
                }
                else if (check != uri)
                {
                    Navigate(check);
                    return true;
                }

                UpdateUrlBar(uri);
                ReadingList?.PageChange(uri);

                if (check.PathAndQuery == CurrentUri.PathAndQuery && check.Fragment != CurrentUri.Fragment)
                {
                    ShowPrevPageIndicator = 0;
                    ShowNextPageIndicator = 0;
                    LeftOffset = 0;
                    return false;
                }
            }

            try
            {
                cancelInject?.Cancel();
                cancelInject = null;
            }
            catch (ObjectDisposedException)
            {

            }

            TitleEx = "Loading...";

            JumpToLastLocationEnabled = false;
            HideContextMenu();

            nextPage = null;
            prevPage = null;
            BackCommand.IsEnabled = ToolbarCanGoBack;
            ForwardCommand.IsEnabled = ToolbarCanGoForward;
            ShowPrevPageIndicator = 0;
            ShowNextPageIndicator = 0;
            currentLocation = null;
            currentSavedLocation = null;
            ForceSetLocationCommand.IsEnabled = false;
            helper?.Reset();
            return false;
        }

        void OnContentLoading()
        {
        }

        private void OnContentLoaded()
        {
            JumpToLastLocationEnabled = false;
            HideContextMenu();

            UpdateUrlBar(CurrentUri);
            ReadingList?.PageChange(CurrentUri);

            nextPage = null;
            prevPage = null;
            BackCommand.IsEnabled = ToolbarCanGoBack;
            ForwardCommand.IsEnabled = ToolbarCanGoForward;
            ShowPrevPageIndicator = 0;
            ShowNextPageIndicator = 0;
            currentLocation = null;
            currentSavedLocation = null;
            ForceSetLocationCommand.IsEnabled = false;
            helper?.Reset();

            InjectScripts();
        }

        async void InjectScripts()
        {
            try
            {
                cancelInject?.Cancel();
                cancelInject = null;
            }
            catch (ObjectDisposedException)
            {

            }

            var cts = cancelInject = new CancellationTokenSource();

            try
            {
                var ct = cts.Token;
                ct.ThrowIfCancellationRequested();
                await OnInjectingScripts(ct);

                foreach (string s in Injections)
                {
                    ct.ThrowIfCancellationRequested();
                    var content = await ReadFile(s, ct);

                    ct.ThrowIfCancellationRequested();
                    switch (System.IO.Path.GetExtension(s))
                    {
                        case ".js":
                            await EvaluateJavascriptAsync(content + "\n//# sourceURL=" + s);
                            break;

                        case ".css":
                            await CallJavascriptAsync("Ao3Track.Marshal.InjectCSS", content);
                            break;
                    }
                }

                ct.ThrowIfCancellationRequested();
            }
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    if (!(e is OperationCanceledException))
                    {
                        App.Log(e);
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception e)
            {
                App.Log(e);
            }
            finally
            {
                cts.Dispose();
            }
        }


        void InitSettings()
        {
            bool b;
            UnitConvSetting uc = UnitConvSetting.None;

            App.Database.GetVariableEvents("UnitConvOptions.temp").Updated += SettingsVariable_Updated;
            App.Database.TryGetVariable("UnitConvOptions.temp", Enum.TryParse, out uc);
            settings.unitConv.temp = uc;

            App.Database.GetVariableEvents("UnitConvOptions.dist").Updated += SettingsVariable_Updated;
            App.Database.TryGetVariable("UnitConvOptions.dist", Enum.TryParse, out uc);
            settings.unitConv.dist = uc;

            App.Database.GetVariableEvents("UnitConvOptions.volume").Updated += SettingsVariable_Updated;
            App.Database.TryGetVariable("UnitConvOptions.volume", Enum.TryParse, out uc);
            settings.unitConv.volume = uc;

            App.Database.GetVariableEvents("UnitConvOptions.weight").Updated += SettingsVariable_Updated;
            App.Database.TryGetVariable("UnitConvOptions.weight", Enum.TryParse, out uc);
            settings.unitConv.weight = uc;

            App.Database.GetVariableEvents("TagOptions.showCatTags").Updated += SettingsVariable_Updated;
            App.Database.TryGetVariable("TagOptions.showCatTags", bool.TryParse, out b);
            settings.tags.showCatTags = b;

            App.Database.GetVariableEvents("TagOptions.showWIPTags").Updated += SettingsVariable_Updated;
            App.Database.TryGetVariable("TagOptions.showWIPTags", bool.TryParse, out b);
            settings.tags.showWIPTags = b;

            App.Database.GetVariableEvents("TagOptions.showRatingTags").Updated += SettingsVariable_Updated;
            App.Database.TryGetVariable("TagOptions.showRatingTags", bool.TryParse, out b);
            settings.tags.showRatingTags = b;

            App.Database.GetVariableEvents("ListFiltering.HideWorks").Updated += SettingsVariable_Updated;
            App.Database.TryGetVariable("ListFiltering.HideWorks", bool.TryParse, out b, false);
            settings.listFiltering.hideFilteredWorks = b;
        }

        private void SettingsVariable_Updated(object sender, Ao3TrackDatabase.VariableUpdatedEventArgs e)
        {
            bool b = false;
            UnitConvSetting uc = UnitConvSetting.None;

            switch (e.VarName)
            {
                case "UnitConvOptions.temp":
                    if (!Enum.TryParse(e.NewValue, out uc)) uc = UnitConvSetting.None;
                    settings.unitConv.temp = uc;
                    break;

                case "UnitConvOptions.dist":
                    if (!Enum.TryParse(e.NewValue, out uc)) uc = UnitConvSetting.None;
                    settings.unitConv.dist = uc;
                    break;

                case "UnitConvOptions.volume":
                    if (!Enum.TryParse(e.NewValue, out uc)) uc = UnitConvSetting.None;
                    settings.unitConv.volume = uc;
                    break;

                case "UnitConvOptions.weight":
                    if (!Enum.TryParse(e.NewValue, out uc)) uc = UnitConvSetting.None;
                    settings.unitConv.weight = uc;
                    break;

                case "TagOptions.showCatTags":
                    bool.TryParse(e.NewValue, out b);
                    settings.tags.showCatTags = b;
                    break;

                case "TagOptions.showWIPTags":
                    bool.TryParse(e.NewValue, out b);
                    settings.tags.showWIPTags = b;
                    break;

                case "TagOptions.showRatingTags":
                    bool.TryParse(e.NewValue, out b);
                    settings.tags.showRatingTags = b;
                    break;

                case "ListFiltering.HideWorks":
                    bool.TryParse(e.NewValue, out b);
                    settings.listFiltering.hideFilteredWorks = b;
                    break;
            }
        }

        Settings settings = new Settings();
        public ISettings Settings => settings;

        public async void OpenExternal(Uri uri)
        {
            var result = await DisplayAlert("External Link", "Open external link in Web Browser?\n\n" + uri.AbsoluteUri, "Yes", "No");
            if (result) Device.OpenUri(uri);
        }

        public void JavascriptError(string name, string message, string url, int lineNo, int coloumNo, string stack)
        {
            App.Log(new JavascriptException(message, stack)
            {
                Name = name,
                Url = url,
                Line = lineNo,
                Column = coloumNo
            });
        }

        CancellationTokenSource _cancelShowErrorHide;
        public void ShowError(string message)
        {
            DoOnMainThreadAsync(async () =>
            {
                if (_cancelShowErrorHide != null) _cancelShowErrorHide.Cancel();
                var cancel = _cancelShowErrorHide = new CancellationTokenSource();

                try
                {
                    ErrorBar.Text = message;
                    ErrorBar.HeightRequest = (double)App.Current.Resources["MediumSmallFontSize"] + 10.0;
                    ErrorBar.IsVisible = true;

                    var token = cancel.Token;
                    await Task.Delay(8000, token);

                    var awaiter = new TaskCompletionSource<bool>();

                    ErrorBar.Animate("slideout", easing: Easing.CubicIn, length: 1000, start: 1.0, end: 0.0,
                        callback: (f) =>
                        {
                            if (token.IsCancellationRequested)
                            {
                                awaiter.TrySetCanceled(token);
                            }
                            else
                            {
                                ErrorBar.HeightRequest = ((double)App.Current.Resources["MediumSmallFontSize"] + 10.0) * f;
                            }
                        },
                        finished: (f, cancelled) =>
                        {
                            if (cancelled) return;

                            if (token.IsCancellationRequested)
                            {
                                awaiter.TrySetCanceled(token);
                            }
                            else
                            {
                                ErrorBar.IsVisible = false;
                                ErrorBar.HeightRequest = 0;
                            }
                        }
                    );

                    await awaiter.Task;
                }
                catch (TaskCanceledException)
                {
                    ErrorBar.AbortAnimation("slideout");
                }
                finally
                {
                    cancel.Dispose();
                    if (cancel == _cancelShowErrorHide) _cancelShowErrorHide = null;
                }
            }).ConfigureAwait(false);
        }

        string GetErrorPageHtml(string message, Uri uri)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            var html = doc.CreateElement("html");
            doc.DocumentNode.AppendChild(html);

            var head = doc.CreateElement("head");
            html.AppendChild(head);

            var meta = doc.CreateElement("meta");
            meta.Attributes.Add(doc.CreateAttribute("charset", "UTF-8"));
            head.AppendChild(meta);

            var title = doc.CreateElement("title");
            title.AppendChild(doc.CreateTextNode("Error loading page"));
            head.AppendChild(title);

            var style = doc.CreateElement("style");
            style.AppendChild(doc.CreateTextNode($@"
        body {{ 
            color: { Ao3TrackReader.Resources.Colors.Base.MediumHigh.ToHex() }; 
            background: { Ao3TrackReader.Resources.Colors.Alt.Medium.ToHex() }; 
        }}
        h1, h2, h3, h4, h5, h6, h7, h8, a {{ 
            color: { Ao3TrackReader.Resources.Colors.Highlight.High.ToHex() }; 
        }}
        details p {{ 
            color: { Ao3TrackReader.Resources.Colors.Base.MediumLow.ToHex() }; 
        }}
"));
            head.AppendChild(style);

            var body = doc.CreateElement("body");
            html.AppendChild(body);

            var heading = doc.CreateElement("h1");
            heading.AppendChild(doc.CreateTextNode("Error loading page"));
            body.AppendChild(heading);

            var details = doc.CreateElement("details");
            var summary = doc.CreateElement("summary");
            summary.AppendChild(doc.CreateTextNode("Unable to navigate to " + uri.AbsoluteUri.HtmlEncode()));
            details.AppendChild(summary);
            var para = doc.CreateElement("p");
            para.AppendChild(doc.CreateTextNode("Reason: " + message.HtmlEncode()));
            details.AppendChild(para);
            body.AppendChild(details);

            para = doc.CreateElement("p");
            var link = doc.CreateElement("a");
            link.Attributes.Add(doc.CreateAttribute("href", uri.AbsoluteUri.HtmlEncode()));
            link.AppendChild(doc.CreateTextNode("Reload Page"));
            para.AppendChild(link);

            para.AppendChild(doc.CreateTextNode(" - "));

            link = doc.CreateElement("a");
            link.Attributes.Add(doc.CreateAttribute("href", uri.Scheme + "://archiveofourown.org/"));
            link.AppendChild(doc.CreateTextNode("Go Home"));
            para.AppendChild(link);

            body.AppendChild(para);


            var writer = new System.IO.StringWriter();
            doc.Save(writer);
            return "<!DOCTYPE html>\n" + writer.ToString();
        }

        public Task<string> ShouldFilterWorkAsync(long workId, IEnumerable<string> workauthors, IEnumerable<string> worktags, IEnumerable<long> workserieses)
        {
            return Task.Run(() => Data.ListFiltering.Instance.ShouldFilterWork(workId, workauthors, worktags, workserieses));
        }
    }
}
