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
using System.Threading.Tasks;
using Xamarin.Forms;
using Ao3TrackReader.Helper;
using Ao3TrackReader.Data;
using System.Threading;
using System.Text.RegularExpressions;
using Ao3TrackReader.Controls;
using Ao3TrackReader.Resources;
using Icons = Ao3TrackReader.Resources.Icons;

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
    public partial class WebViewPage : ContentPage, IWebViewPage, IPageEx, IWebViewPageNative
    {
        IAo3TrackHelper helper;

        //public IList<ToolbarItem> Toolbar => new List<ToolbarItem>(20);

        public IEnumerable<Models.IHelpInfo> HelpItems {
            get { return ((IEnumerable<Models.IHelpInfo>)AllToolbarItems).Concat(ExtraHelp); }
        }

        List<KeyValuePair<string, DisableableCommand<string>>> ContextMenuItems { get; set; }
        DisableableCommand<string> ContextMenuOpenAdd;
        DisableableCommand<string> ContextMenuAdd;

        public ReadingListView ReadingList { get { return ReadingListPane; } }

        public WebViewPage()
        {
            InitializeToolbarCommands();          

            TitleEx = "Loading...";

            InitializeComponent();

            UpdateBackButton();

            foreach (var tbi in AllToolbarItems)
            {
                tbi.PropertyChanged += ToolBarItem_PropertyChanged;
            }

            HelpPane.Init();
            SetupContextMenu();
            UpdateToolbar();

            WebViewHolder.Content = CreateWebView();

            string url = App.Database.GetVariable("Sleep:URI");
            App.Database.DeleteVariable("Sleep:URI");

            Uri uri = null;
            if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = Data.Ao3SiteDataLookup.CheckUri(uri);

            if (uri == null) uri = new Uri("http://archiveofourown.org/");

            // retore font size!
            if (!App.Database.TryGetVariable("LogFontSize", int.TryParse, out int lfs)) LogFontSize = lfs;
            else LogFontSize = 0;

            App.Database.TryGetVariable("ToolbarBackBehaviour", Enum.TryParse<NavigateBehaviour>, out ToolbarBackBehaviour, NavigateBehaviour.History);
            App.Database.TryGetVariable("ToolbarForwardBehaviour", Enum.TryParse<NavigateBehaviour>, out ToolbarForwardBehaviour, NavigateBehaviour.HistoryThenPage);
            App.Database.TryGetVariable("SwipeBackBehaviour", Enum.TryParse<NavigateBehaviour>, out SwipeBackBehaviour, NavigateBehaviour.History);
            App.Database.TryGetVariable("SwipeForwardBehaviour", Enum.TryParse<NavigateBehaviour>, out SwipeForwardBehaviour, NavigateBehaviour.PageThenHistory);

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                Navigate(uri);
            });
        }

        private void UrlBar_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "IsVisible")
            {
                if (urlBar.IsVisible == false) UrlBarToolBarItem.Foreground = Xamarin.Forms.Color.Default;
                else UrlBarToolBarItem.Foreground = Colors.Highlight.High;
            }
        }

        private void ReadingList_IsOnScreenChanged(object sender, bool onscreen)
        {
            if (!onscreen) ReadingListToolBarItem.Foreground = Xamarin.Forms.Color.Default;
            else ReadingListToolBarItem.Foreground = Colors.Highlight.High;
        }

        private void SettingsPane_IsOnScreenChanged(object sender, bool onscreen)
        {
            if (!onscreen) SettingsToolBarItem.Foreground = Xamarin.Forms.Color.Default;
            else SettingsToolBarItem.Foreground = Colors.Highlight.High;
        }

        private void HelpPane_IsOnScreenChanged(object sender, bool onscreen)
        {
            if (!onscreen) HelpToolBarItem.Foreground = Xamarin.Forms.Color.Default;
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
        public DisableableCommand AddToReadingListCommand { get; private set; }
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

            SyncCommand = new DisableableCommand(() => App.Storage.dosync(true), !App.Storage.IsSyncing && App.Storage.CanSync);
            App.Storage.BeginSyncEvent += (sender, e) => DoOnMainThread(() => SyncCommand.IsEnabled = false);
            App.Storage.EndSyncEvent += (sender, e) => DoOnMainThread(() => SyncCommand.IsEnabled = !App.Storage.IsSyncing && App.Storage.CanSync);
            SyncCommand.IsEnabled = !App.Storage.IsSyncing && App.Storage.CanSync;

            RefreshCommand = new DisableableCommand(Refresh);
            ReadingListCommand = new DisableableCommand(() => TogglePane(ReadingList));
            AddToReadingListCommand = new DisableableCommand(() => ReadingList.AddAsync(CurrentUri.AbsoluteUri));

            UrlBarCommand = new DisableableCommand(() =>
            {
                if (urlBar.IsVisible)
                {
                    urlBar.IsVisible = false;
                    urlBar.Unfocus();
                }
                else
                {
                    urlEntry.Text = CurrentUri.AbsoluteUri;
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
            var mode = App.GetInteractionMode();

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

                new KeyValuePair<string, DisableableCommand<string>>("Copy Link", new DisableableCommand<string>((url) =>
                {
                    CopyToClipboard(url, "url");
                }))
            };
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            UpdateBackButton();
        }

        public virtual void OnSleep()
        {
            var loc = CurrentLocation;
            var uri = CurrentUri;
            if (loc != null)
            {
                uri = new Uri(uri, "#ao3tjump:" + loc.number.ToString() + ":" + loc.chapterid.ToString() + (loc.location == null ? "" : (":" + loc.location.ToString())));
            }
            App.Database.SaveVariable("Sleep:URI", uri.AbsoluteUri);
        }

        public virtual void OnResume()
        {
            App.Database.DeleteVariable("Sleep:URI");
        }

        public void NavigateToLast(long workid, bool fullwork)
        {
            Task.Run(async () =>
            {
                var workchaps = await App.Storage.getWorkChaptersAsync(new[] { workid });

                DoOnMainThread(() =>
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
            });
        }

        public void Navigate(long workid, bool fullwork)
        {
            DoOnMainThread(() =>
            {
                UriBuilder uri = new UriBuilder("http://archiveofourown.org/works/" + workid.ToString());
                if (fullwork) uri.Query = "view_full_work=true";
                uri.Fragment = "ao3tjump";
                Navigate(uri.Uri);
            });
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
                var uri = Ao3SiteDataLookup.CheckUri(new Uri(urlEntry.Text));
                if (uri != null)
                {
                    Navigate(uri);
                }
                else
                {
                    await DisplayAlert("Url error", "Can only enter urls on archiveofourown.org", "Ok");
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
        public PageTitle PageTitle {
            get { return pageTitle; }
            set {
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

                DoOnMainThread(() =>
                {
                    FontDecreaseCommand.IsEnabled = log_font_size > LogFontSizeMin;
                    FontIncreaseCommand.IsEnabled = log_font_size < LogFontSizeMax;
                    helper?.OnAlterFontSize(FontSize);
                });
            }
        }
        public int FontSize
        {
            get { return (int) Math.Round(100.0 * Math.Pow(1.05,LogFontSize),MidpointRounding.AwayFromZero); }
        }


        static object locker = new object();

        public System.Threading.Tasks.Task<System.Collections.Generic.IDictionary<long, Ao3TrackReader.Helper.WorkChapter>> GetWorkChaptersAsync(long[] works)
        {
            return App.Storage.getWorkChaptersAsync(works);
        }

        public System.Threading.Tasks.Task<System.Collections.Generic.IDictionary<string, bool>> AreUrlsInReadingListAsync(string[] urls)
        {
            return ReadingList.AreUrlsInListAsync(urls);
        }

        public T DoOnMainThread<T>(Func<T> function)
        {
            var task = DoOnMainThreadAsync(function);
            task.Wait();
            return task.Result;
        }
        public async Task<T> DoOnMainThreadAsync<T>(Func<T> function)
        {
            if (IsMainThread)
            {
                return function();
            }
            else
            {
                var cs = new TaskCompletionSource<T>();

                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    cs.SetResult(function());
                });
                return await cs.Task;
            }
        }

        object IWebViewPage.DoOnMainThread(MainThreadFunc function)
        {
            var task = DoOnMainThreadAsync(() => function());
            task.Wait();
            return task.Result;
        }

        public async void DoOnMainThread(MainThreadAction function)
        {
            await DoOnMainThreadAsync(() => function());
        }

        public async Task DoOnMainThreadAsync(MainThreadAction function)
        {
            if (IsMainThread)
            {
                function();
            }
            else
            {
                var task = new Task(() => { });
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    function();
                    task.Start();
                });
                await task;
            }
        }


        public void SetWorkChapters(IDictionary<long, WorkChapter> works)
        {
            App.Storage.setWorkChapters(works);

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
            Task.Run(() =>
            {
                helper.OnJumpToLastLocation(true);
            });
        }

        public bool JumpToLastLocationEnabled
        {
            set
            {
                if (JumpCommand != null) JumpCommand.IsEnabled = value;
            }
            get
            {
                return JumpCommand?.IsEnabled ?? false;
            }
        }

        public int ShowPrevPageIndicator
        {
            get {
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
            get {
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
                h1 = behaviour.HasFlag(NavigateBehaviour.History); ;
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
        public IWorkChapterEx CurrentLocation {
            get { return currentLocation; }
            set {
                lock (currentLocationLock)
                {
                    currentLocation = value;
                    if (currentLocation != null && currentLocation.workid == currentSavedLocation?.workid)
                    {
                        ForceSetLocationCommand.IsEnabled = currentLocation.LessThan(currentSavedLocation);
                        return;
                    }
                    else if (currentLocation == null)
                    {
                        ForceSetLocationCommand.IsEnabled = false;
                        return;
                    }

                }
                Task.Run(async () =>
                {
                    long workid = 0;
                    lock(currentLocationLock)
                    {
                        if (currentLocation != null) workid = currentLocation.workid;
                    }
                    var workchap = workid == 0 ? null : (await App.Storage.getWorkChaptersAsync(new[] { workid })).Select(kvp => kvp.Value).FirstOrDefault();
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
                        }
                    });
                }
            }
        }


        public void ForceSetLocation()
        {
            if (currentLocation != null)
            {
                Task.Run(async () =>
                {
                    var db_workchap = (await App.Storage.getWorkChaptersAsync(new[] { currentLocation.workid })).Select((kp)=>kp.Value).FirstOrDefault();
                    currentLocation = currentSavedLocation = new WorkChapter(currentLocation) { seq = (db_workchap?.seq ?? 0) + 1 };
                    App.Storage.setWorkChapters(new Dictionary<long, WorkChapter> { [currentLocation.workid] = currentSavedLocation });
                });
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
            if ((!SwipeCanGoBack && offset > 0.0) || (!SwipeCanGoForward && offset< 0.0)) {
                offset = 0.0;
            }
            else if (offset< -end) 
            {
                offset = -end;
            }
            else if (offset > end) 
            {
                offset = end;
            }
            LeftOffset = offset;
            

            if (SwipeCanGoForward && offset< -centre) {
                ShowNextPageIndicator = 2;
                if (offset <= -end) return -3;
                return -2;
            }
            else if (SwipeCanGoForward && offset< 0) {
                ShowNextPageIndicator = 1;
            }
            else {
                ShowNextPageIndicator = 0;
            }

            if (SwipeCanGoBack && offset >= centre) {
                ShowPrevPageIndicator = 2;
                if (offset >= end) return 3;
                return 2;
            }
            else if (SwipeCanGoBack && offset > 0)
            {
                ShowPrevPageIndicator = 1;
            }
            else {
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
                () => {
                    if (!webViewDragAccelerateStopWatch.IsRunning) return false;

                    double now = webViewDragAccelerateStopWatch.ElapsedMilliseconds;

                    double acceleration = 0;   // pixels/s^2
                    if (offsetCat <= -2) acceleration = -3000.0;
                    else if (offsetCat == -1) acceleration = 3000.0;
                    else if (offsetCat >= 2) acceleration = 3000.0;
                    else if (offsetCat == 1) acceleration = -3000.0;
                    else {
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
                    if (offsetCat == 3) {
                        SwipeGoBack();
                        return false;
                    }
                    else if (offsetCat == -3) {
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
            return await EvaluateJavascriptAsync(function + "(" + string.Join(",", args) + ");");
        }

        private void OnWebViewGotFocus()
        {
            TogglePane(null);
        }

        private CancellationTokenSource cancelInject;
        private bool OnNavigationStarting(Uri uri)
        {
            var check = Ao3SiteDataLookup.CheckUri(uri);
            if (check == null)
            {
                // Handle external uri
                LeftOffset = 0;
                return true;
            }
            else if (check != uri)
            {
                Navigate(check);
                return true;
            }

            if (check.PathAndQuery == CurrentUri.PathAndQuery && check.Fragment != CurrentUri.Fragment)
            {
                return false;
            }

            try
            {
                cancelInject?.Cancel();
            }
            catch (ObjectDisposedException)
            {

            }

            cancelInject = new CancellationTokenSource();
            TitleEx = "Loading...";

            JumpCommand.IsEnabled = false;
            HideContextMenu();

            if (urlEntry != null) urlEntry.Text = uri.AbsoluteUri;
            ReadingList?.PageChange(uri);

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
            JumpCommand.IsEnabled = false;
            HideContextMenu();

            if (urlEntry != null) urlEntry.Text = CurrentUri.AbsoluteUri;
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

            InjectScripts(cancelInject);
        }

        async void InjectScripts(CancellationTokenSource cts)
        {
            try
            {
                var ct = cts.Token;
                ct.ThrowIfCancellationRequested();
                await OnInjectingScripts(ct);

                foreach (string s in ScriptsToInject)
                {
                    ct.ThrowIfCancellationRequested();
                    var content = await ReadFile(s, ct);

                    ct.ThrowIfCancellationRequested();
                    await EvaluateJavascriptAsync(content + "\n//# sourceURL=" + s);
                }

                foreach (string s in CssToInject)
                {
                    ct.ThrowIfCancellationRequested();
                    var content = await ReadFile(s, ct);

                    ct.ThrowIfCancellationRequested();
                    await CallJavascriptAsync("Ao3Track.InjectCSS", content);
                }

                ct.ThrowIfCancellationRequested();
                await OnInjectedScripts(ct);
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

        public IUnitConvOptions UnitConvOptions {
            get
            {
                var r = new Models.UnitConvOptions();

                bool? v;
                if (App.Database.TryGetVariable("UnitConvOptions.tempToC", bool.TryParse, out v)) r.tempToC = v;
                if (App.Database.TryGetVariable("UnitConvOptions.distToM", bool.TryParse, out v)) r.distToM = v;
                if (App.Database.TryGetVariable("UnitConvOptions.volumeToM", bool.TryParse, out v)) r.volumeToM = v;
                if (App.Database.TryGetVariable("UnitConvOptions.weightToM", bool.TryParse, out v)) r.weightToM = v;

                return r;
            }
        }

    }
}
