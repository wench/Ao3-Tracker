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

namespace Ao3TrackReader
{
    public partial class WebViewPage : ContentPage, IWebViewPage, IPageEx, IWebViewPageNative
    {
        Ao3TrackHelper helper;

        ToolbarItem settingsToolBarItem;
        ToolbarItem readingListToolBarItem;
        ToolbarItem urlBarToolBarItem;

        DisableableCommand jumpButton { get; set; }
        DisableableCommand incFontSizeButton { get; set; }
        DisableableCommand decFontSizeButton { get; set; }
        DisableableCommand nextPageButton { get; set; }
        DisableableCommand prevPageButton { get; set; }
        DisableableCommand syncButton { get; set; }
        DisableableCommand forceSetLocationButton { get; set; }

        public ReadingListView ReadingList { get; private set; }
        public SettingsView SettingsPane { get; private set; }

        public WebViewPage()
        {
            TitleEx = "Loading...";
            InitializeComponent();           

            SetupToolbarCommands();
            SetupToolbar();

            Panes.Children.Add(SettingsPane = new SettingsView(this));
            Panes.Children.Add(ReadingList = new ReadingListView(this));

            SettingsPane.IsOnScreenChanged += SettingsPane_IsOnScreenChanged;
            ReadingList.IsOnScreenChanged += ReadingList_IsOnScreenChanged;
            urlBar.PropertyChanged += UrlBar_PropertyChanged;

            WebViewHolder.Content = CreateWebView();

            string url = App.Database.GetVariable("Sleep:URI");
            App.Database.DeleteVariable("Sleep:URI");

            Uri uri = null;
            if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = Data.Ao3SiteDataLookup.CheckUri(new Uri(uri, "#ao3t:jump"));

            if (uri == null) uri = new Uri("http://archiveofourown.org/");

            // retore font size!
            if (!App.Database.TryGetVariable("FontSize", int.TryParse, out font_size)) FontSize = 100;

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                Navigate(uri);
            });
        }

        private void UrlBar_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "IsVisible")
            {
                if (readingListToolBarItem == null) return;
                if (urlBar.IsVisible == false) urlBarToolBarItem.Foreground = Xamarin.Forms.Color.Default;
                else urlBarToolBarItem.Foreground = Colors.Highlight.High;
            }
        }

        private void ReadingList_IsOnScreenChanged(object sender, bool e)
        {
            if (readingListToolBarItem == null) return;
            if (e == false) readingListToolBarItem.Foreground = Xamarin.Forms.Color.Default;
            else readingListToolBarItem.Foreground = Colors.Highlight.High;
        }

        private void SettingsPane_IsOnScreenChanged(object sender, bool e)
        {
            if (settingsToolBarItem == null) return;
            if (e == false) settingsToolBarItem.Foreground = Xamarin.Forms.Color.Default;
            else settingsToolBarItem.Foreground = Colors.Highlight.High;
        }

        void SetupToolbarCommands()
        {
            prevPageButton = new DisableableCommand(GoBack, false);
            nextPageButton = new DisableableCommand(GoForward, false);
            jumpButton = new DisableableCommand(OnJumpClicked, false);
            incFontSizeButton = new DisableableCommand(() => FontSize += 10);
            decFontSizeButton = new DisableableCommand(() => FontSize -= 10);
            syncButton = new DisableableCommand(() => App.Storage.dosync(true), !App.Storage.IsSyncing && App.Storage.CanSync);
            App.Storage.BeginSyncEvent += (sender, e) => DoOnMainThread(() => syncButton.IsEnabled = false);
            App.Storage.EndSyncEvent += (sender, e) => DoOnMainThread(() => syncButton.IsEnabled = !App.Storage.IsSyncing && App.Storage.CanSync);
            syncButton.IsEnabled = !App.Storage.IsSyncing && App.Storage.CanSync;
            forceSetLocationButton = new DisableableCommand(ForceSetLocation);
        }

        void SetupToolbar()
        {
            if (ShowBackOnToolbar)
            {
                ToolbarItems.Add(new ToolbarItem
                {
                    Text = "Back",
                    Icon = Icons.Back,
                    Command = prevPageButton
                });
            }

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Forward",
                Icon = Icons.Forward,
                Command = nextPageButton
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Refresh",
                Icon = Icons.Refresh,
                Command = new Command(Refresh)
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Jump",
                Icon = Icons.Redo,
                Command = jumpButton
            });

            ToolbarItems.Add(readingListToolBarItem = new ToolbarItem
            {
                Text = "Reading List",
                Icon = Icons.Bookmarks,
                Command = new Command(() =>
                {
                    SettingsPane.IsOnScreen = false;
                    ReadingList.IsOnScreen = !ReadingList.IsOnScreen;
                })
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Add to Reading List",
                Icon = Icons.AddPage,
                Command = new Command(() =>
                {
                    ReadingList.AddAsync(CurrentUri.AbsoluteUri);
                })
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Font Increase",
                Icon = Icons.FontUp,
                Command = incFontSizeButton
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Font Decrease",
                Icon = Icons.FontDown,
                Command = decFontSizeButton
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Sync",
                Icon = Icons.Sync,
                Command = syncButton
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Force set location",
                Icon = Icons.ForceLoc,
                Command = forceSetLocationButton
            });

            ToolbarItems.Add(urlBarToolBarItem = new ToolbarItem
            {
                Text = "Url Bar",
                Icon = Icons.Rename,
                Command = new Command(() =>
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
                })
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Reset Font Size",
                Icon = Icons.Font,
                Command = new Command(() => FontSize = 100)
            });
            ToolbarItems.Add(settingsToolBarItem = new ToolbarItem
            {
                Text = "Settings",
                Icon = Icons.Settings,
                Command = new Command(() =>
                {
                    ReadingList.IsOnScreen = false;
                    SettingsPane.IsOnScreen = !SettingsPane.IsOnScreen;
                })
            });
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                if (ToolbarItems.Count == 0)
                {
                    SetupToolbar();
                }
#if !WINDOWS_UWP
                else 
                {
                    int onscreen = ((int)width - 60) / 48;
                    var items = ToolbarItems;

                    for (var i = 0; i < onscreen && i < ToolbarItems.Count; i++)
                    {
                        items[i].Order = ToolbarItemOrder.Primary;
                    }
                    for (var i = onscreen; i < ToolbarItems.Count; i++)
                    {
                        items[i].Order = ToolbarItemOrder.Secondary;
                    }
                    var item = items[items.Count - 1];
                    items.RemoveAt(items.Count - 1);
                    items.Add(item);
                }
#endif
            });
        }

        public virtual void OnSleep()
        {
            App.Database.SaveVariable("Sleep:URI", CurrentUri.AbsoluteUri);
        }

        public virtual void OnResume()
        {
            App.Database.DeleteVariable("Sleep:URI");
        }

        public void NavigateToLast(long workid)
        {
            Task.Run(async () =>
            {
                var workchaps = await App.Storage.getWorkChaptersAsync(new[] { workid });

                DoOnMainThread(() =>
                {
                    WorkChapter wc;
                    if (workchaps.TryGetValue(workid, out wc) && wc.Chapterid != 0)
                    {
                        Navigate(new Uri(string.Concat("http://archiveofourown.org/works/", workid, "/chapters/", wc.Chapterid, "#ao3t:jump")));
                    }
                    else
                    {
                        Navigate(new Uri(string.Concat("http://archiveofourown.org/works/", workid, "#ao3t:jump")));
                    }
                });
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

        public Models.TextTree TitleEx
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

                var ts = new Models.Span();

                ts.Nodes.Add(pageTitle.Title);

                if (pageTitle.Authors != null && pageTitle.Authors.Length != 0)
                {
                    ts.Nodes.Add(new Models.TextNode { Text = " by ", Foreground = Colors.Base });

                    bool first = true;
                    foreach (var user in pageTitle.Authors)
                    {
                        if (!first)
                            ts.Nodes.Add(new Models.TextNode { Text = ", ", Foreground = Colors.Base });
                        else
                            first = false;

                        ts.Nodes.Add(user.Replace(' ', '\xA0'));
                    }
                }

                if (!string.IsNullOrWhiteSpace(pageTitle.Chapter) || !string.IsNullOrWhiteSpace(pageTitle.Chaptername))
                {
                    ts.Nodes.Add(new Models.TextNode { Text = " | ", Foreground = Colors.Base });

                    if (!string.IsNullOrWhiteSpace(pageTitle.Chapter))
                    {
                        ts.Nodes.Add(pageTitle.Chapter.Replace(' ', '\xA0'));

                        if (!string.IsNullOrWhiteSpace(pageTitle.Chaptername))
                            ts.Nodes.Add(new Models.TextNode { Text = ": ", Foreground = Colors.Base });
                    }
                    if (!string.IsNullOrWhiteSpace(pageTitle.Chaptername))
                        ts.Nodes.Add(pageTitle.Chaptername.Replace(' ', '\xA0'));
                }

                if (!string.IsNullOrWhiteSpace(pageTitle.Primarytag))
                {
                    ts.Nodes.Add(new Models.TextNode { Text = " | ", Foreground = Colors.Base });
                    ts.Nodes.Add(pageTitle.Primarytag.Replace(' ', '\xA0'));
                }

                if (pageTitle.Fandoms != null && pageTitle.Fandoms.Length != 0)
                {
                    ts.Nodes.Add(new Models.TextNode { Text = " | ", Foreground = Colors.Base });
                    
                    bool first = true;
                    foreach (var fandom in pageTitle.Fandoms)
                    {
                        if (!first)
                            ts.Nodes.Add(new Models.TextNode { Text = ", ", Foreground = Colors.Base });
                        else
                            first = false;

                        ts.Nodes.Add(fandom.Replace(' ', '\xA0'));
                    }
                }

                TitleEx = ts;
            }
        }

        public int FontSizeMax { get { return 300; } }
        public int FontSizeMin { get { return 10; } }
        private int font_size = 0;
        public int FontSize
        {
            get { return font_size; }
            set
            {
                if (font_size != value) 
                {
                    Task.Run(() =>
                    {
                        App.Database.SaveVariable("FontSize", value);
                    });
                }

                font_size = value;
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    decFontSizeButton.IsEnabled = FontSize > FontSizeMin;
                    incFontSizeButton.IsEnabled = FontSize < FontSizeMax;
                });
                helper?.OnAlterFontSize();
            }
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

        public void DoOnMainThread(MainThreadAction function)
        {
            DoOnMainThreadAsync(() => function()).Wait();
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
                if (currentSavedLocation != null && currentLocation != null && works.TryGetValue(currentLocation.Workid, out var workchap))
                {
                    workchap.Workid = currentLocation.Workid;
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
                if (jumpButton != null) jumpButton.IsEnabled = value;
            }
            get
            {
                return jumpButton?.IsEnabled ?? false;
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
                nextPageButton.IsEnabled = CanGoForward;
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
                prevPageButton.IsEnabled = CanGoBack;
            }
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
            if (SettingsPane.IsOnScreen)
            {
                SettingsPane.IsOnScreen = false;
                return true;
            }
            else if (ReadingList.IsOnScreen)
            {
                ReadingList.IsOnScreen = false;
                return true;
            }
            else if (CanGoBack)
            {
                GoBack();
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
                    if (currentLocation != null && currentLocation.Workid == currentSavedLocation?.Workid)
                    {
                        forceSetLocationButton.IsEnabled = currentLocation.LessThan(currentSavedLocation);
                        return;
                    }
                    else if (currentLocation == null)
                    {
                        forceSetLocationButton.IsEnabled = false;
                        return;
                    }

                }
                Task.Run(async () =>
                {
                    long workid = 0;
                    lock(currentLocationLock)
                    {
                        if (currentLocation != null) workid = currentLocation.Workid;
                    }
                    var workchap = workid == 0 ? null : (await App.Storage.getWorkChaptersAsync(new[] { workid })).Select(kvp => kvp.Value).FirstOrDefault();
                    if (workchap != null) workchap.Workid = workid;
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
                            if (currentLocation != null && currentLocation.Workid == currentSavedLocation?.Workid)
                                forceSetLocationButton.IsEnabled = currentLocation.LessThan(currentSavedLocation);
                            else
                                forceSetLocationButton.IsEnabled = false;
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
                    var db_workchap = (await App.Storage.getWorkChaptersAsync(new[] { currentLocation.Workid })).Select((kp)=>kp.Value).FirstOrDefault();
                    currentLocation = currentSavedLocation = new WorkChapter(currentLocation) { Seq = (db_workchap?.Seq ?? 0) + 1 };
                    App.Storage.setWorkChapters(new Dictionary<long, WorkChapter> { [currentLocation.Workid] = currentSavedLocation });
                });
            }
        }


        System.Diagnostics.Stopwatch webViewDragAccelerateStopWatch = null;
        public void StopWebViewDragAccelerate()
        {
            if (webViewDragAccelerateStopWatch != null)
                webViewDragAccelerateStopWatch.Reset();
        }

        int swipeOffsetChanged(double offset) 
        {
            var end = WebViewHolder.Width;
            var centre = end / 2;
            if ((!CanGoBack && offset > 0.0) || (!CanGoForward && offset< 0.0)) {
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
            

            if (CanGoForward && offset< -centre) {
                ShowNextPageIndicator = 2;
                if (offset <= -end) return -3;
                return -2;
            }
            else if (CanGoForward && offset< 0) {
                ShowNextPageIndicator = 1;
            }
            else {
                ShowNextPageIndicator = 0;
            }

            if (CanGoBack && offset >= centre) {
                ShowPrevPageIndicator = 2;
                if (offset >= end) return 3;
                return 2;
            }
            else if (CanGoBack && offset > 0)
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
            var offsetCat = swipeOffsetChanged(offset);

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

                    offsetCat = swipeOffsetChanged(offset);
                    if (offsetCat == 3) {
                        GoBack();
                        return false;
                    }
                    else if (offsetCat == -3) {
                        GoForward();
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
                if (type == typeof(bool))
                    args[i] = args[i].ToString().ToLowerInvariant();
                else if (type == typeof(double))
                    args[i] = ((double)args[i]).ToString("r");
                else if (type == typeof(float))
                    args[i] = ((float)args[i]).ToString("r");
                else if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort))
                    args[i] = args[i].ToString();
                else if (type == typeof(string))
                    args[i] = args[i].ToString().ToLiteral();
                else
                    args[i] = Newtonsoft.Json.JsonConvert.SerializeObject(args[i]);
            }
            return await EvaluateJavascriptAsync(function + "(" + string.Join(",", args) + ");");
        }

        private void OnWebViewGotFocus()
        {
            ReadingList.IsOnScreen = false;
            SettingsPane.IsOnScreen = false;
        }

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

            if (check.LocalPath == CurrentUri.LocalPath)
            {
                return false;
            }

            TitleEx = "Loading...";

            jumpButton.IsEnabled = false;
            CloseContextMenu();

            if (urlEntry != null) urlEntry.Text = uri.AbsoluteUri;
            ReadingList?.PageChange(uri);

            nextPage = null;
            prevPage = null;
            prevPageButton.IsEnabled = CanGoBack;
            nextPageButton.IsEnabled = CanGoForward;
            ShowPrevPageIndicator = 0;
            ShowNextPageIndicator = 0;
            currentLocation = null;
            currentSavedLocation = null;
            forceSetLocationButton.IsEnabled = false;
            helper?.Reset();
            return false;
        }

        void OnContentLoading()
        {
            if (urlEntry != null) urlEntry.Text = CurrentUri.AbsoluteUri;
            ReadingList?.PageChange(CurrentUri);
            ShowPrevPageIndicator = 0;
            ShowNextPageIndicator = 0;
            currentLocation = null;
            currentSavedLocation = null;
            forceSetLocationButton.IsEnabled = false;
            helper?.Reset();
            Task.Run(async () =>
            {
                await Task.Delay(300);
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() => LeftOffset = 0);
            });
        }

        private void OnContentLoaded()
        {
            LeftOffset = 0;
            EvaluateJavascriptAsync(JavaScriptInject).Wait(0);
        }

    }
}
