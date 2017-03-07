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

namespace Ao3TrackReader
{
    public partial class WebViewPage : ContentPage, IWebViewPage, IPageEx, IWebViewPageNative
    {
        IAo3TrackHelper helper;

        ToolbarItem SettingsToolBarItem { get; set; }
        ToolbarItem ReadingListToolBarItem { get; set; }
        ToolbarItem UrlBarToolBarItem { get; set; }

        DisableableCommand JumpButton { get; set; }
        DisableableCommand IncFontSizeButton { get; set; }
        DisableableCommand DecFontSizeButton { get; set; }
        DisableableCommand NextPageButton { get; set; }
        DisableableCommand PrevPageButton { get; set; }
        DisableableCommand SyncButton { get; set; }
        DisableableCommand ForceSetLocationButton { get; set; }

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
                uri = Data.Ao3SiteDataLookup.CheckUri(uri);

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
                if (ReadingListToolBarItem == null) return;
                if (urlBar.IsVisible == false) UrlBarToolBarItem.Foreground = Xamarin.Forms.Color.Default;
                else UrlBarToolBarItem.Foreground = Colors.Highlight.High;
            }
        }

        private void ReadingList_IsOnScreenChanged(object sender, bool e)
        {
            if (ReadingListToolBarItem == null) return;
            if (e == false) ReadingListToolBarItem.Foreground = Xamarin.Forms.Color.Default;
            else ReadingListToolBarItem.Foreground = Colors.Highlight.High;
        }

        private void SettingsPane_IsOnScreenChanged(object sender, bool e)
        {
            if (SettingsToolBarItem == null) return;
            if (e == false) SettingsToolBarItem.Foreground = Xamarin.Forms.Color.Default;
            else SettingsToolBarItem.Foreground = Colors.Highlight.High;
        }

        void SetupToolbarCommands()
        {
            PrevPageButton = new DisableableCommand(SwipeGoBack, false);
            NextPageButton = new DisableableCommand(SwipeGoForward, false);
            JumpButton = new DisableableCommand(OnJumpClicked, false);
            IncFontSizeButton = new DisableableCommand(() => FontSize += 10);
            DecFontSizeButton = new DisableableCommand(() => FontSize -= 10);
            SyncButton = new DisableableCommand(() => App.Storage.dosync(true), !App.Storage.IsSyncing && App.Storage.CanSync);
            App.Storage.BeginSyncEvent += (sender, e) => DoOnMainThread(() => SyncButton.IsEnabled = false);
            App.Storage.EndSyncEvent += (sender, e) => DoOnMainThread(() => SyncButton.IsEnabled = !App.Storage.IsSyncing && App.Storage.CanSync);
            SyncButton.IsEnabled = !App.Storage.IsSyncing && App.Storage.CanSync;
            ForceSetLocationButton = new DisableableCommand(ForceSetLocation);
        }

        void SetupToolbar()
        {
            if (ShowBackOnToolbar)
            {
                ToolbarItems.Add(new ToolbarItem
                {
                    Text = "Back",
                    Icon = Icons.Back,
                    Command = PrevPageButton
                });
            }

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Forward",
                Icon = Icons.Forward,
                Command = NextPageButton
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
                Command = JumpButton
            });

            ToolbarItems.Add(ReadingListToolBarItem = new ToolbarItem
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
                Command = IncFontSizeButton
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Font Decrease",
                Icon = Icons.FontDown,
                Command = DecFontSizeButton
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Sync",
                Icon = Icons.Sync,
                Command = SyncButton
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Force set location",
                Icon = Icons.ForceLoc,
                Command = ForceSetLocationButton
            });

            ToolbarItems.Add(UrlBarToolBarItem = new ToolbarItem
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
            ToolbarItems.Add(SettingsToolBarItem = new ToolbarItem
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
#if !WINDOWS_UWP && false
                else 
                {
                    int onscreen = Math.Max(6,((int)width - 60) / 48);
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
            var loc = CurrentLocation;
            var uri = CurrentUri;
            if (loc != null)
            {
                uri = new Uri(uri, "#ao3tjump:" + loc.number.ToString() + ":" + loc.chapterid.ToString() + (loc.location == null? "" : (":" +loc.location.ToString())));
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
                    DecFontSizeButton.IsEnabled = FontSize > FontSizeMin;
                    IncFontSizeButton.IsEnabled = FontSize < FontSizeMax;
                });
                helper?.OnAlterFontSize(value);
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
                if (JumpButton != null) JumpButton.IsEnabled = value;
            }
            get
            {
                return JumpButton?.IsEnabled ?? false;
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
                NextPageButton.IsEnabled = SwipeCanGoForward;
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
                PrevPageButton.IsEnabled = SwipeCanGoBack;
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
            else if (SwipeCanGoBack)
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
                        ForceSetLocationButton.IsEnabled = currentLocation.LessThan(currentSavedLocation);
                        return;
                    }
                    else if (currentLocation == null)
                    {
                        ForceSetLocationButton.IsEnabled = false;
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
                                ForceSetLocationButton.IsEnabled = currentLocation.LessThan(currentSavedLocation);
                            else
                                ForceSetLocationButton.IsEnabled = false;
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

            if (check.PathAndQuery == CurrentUri.PathAndQuery)
            {
                return false;
            }

            TitleEx = "Loading...";

            JumpButton.IsEnabled = false;
            HideContextMenu();

            if (urlEntry != null) urlEntry.Text = uri.AbsoluteUri;
            ReadingList?.PageChange(uri);

            nextPage = null;
            prevPage = null;
            PrevPageButton.IsEnabled = SwipeCanGoBack;
            NextPageButton.IsEnabled = SwipeCanGoForward;
            ShowPrevPageIndicator = 0;
            ShowNextPageIndicator = 0;
            currentLocation = null;
            currentSavedLocation = null;
            ForceSetLocationButton.IsEnabled = false;
            helper?.Reset();
            return false;
        }

        void OnContentLoading()
        {
        }

        private void OnContentLoaded()
        {
            JumpButton.IsEnabled = false;
            HideContextMenu();

            if (urlEntry != null) urlEntry.Text = CurrentUri.AbsoluteUri;
            ReadingList?.PageChange(CurrentUri);

            nextPage = null;
            prevPage = null;
            PrevPageButton.IsEnabled = SwipeCanGoBack;
            NextPageButton.IsEnabled = SwipeCanGoForward;
            ShowPrevPageIndicator = 0;
            ShowNextPageIndicator = 0;
            currentLocation = null;
            currentSavedLocation = null;
            ForceSetLocationButton.IsEnabled = false;
            helper?.Reset();

            InjectScripts();
        }

        async void InjectScripts()
        {
            OnInjectingScripts();

            foreach (string s in ScriptsToInject)
            {
                var content = await ReadFile(s);
                await EvaluateJavascriptAsync(content + "\n//# sourceURL=" + s);
            }
            foreach (string s in CssToInject)
            {
                var content = await ReadFile(s);
                await CallJavascriptAsync("Ao3Track.InjectCSS", content);
            }
            OnInjectedScripts();
        }

    }
}
