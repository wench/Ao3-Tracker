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


namespace Ao3TrackReader
{
    public partial class WebViewPage : ContentPage, IEventHandler, IPageEx
    {
#if WINDOWS_UWP
        public Windows.UI.Core.CoreDispatcher Dispatcher { get; private set; }
#endif
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
                
#if WINDOWS_UWP
            Dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;
#endif

            SetupToolbarCommands();
            SetupToolbar();

            Panes.Children.Add(SettingsPane = new SettingsView(this));
            Panes.Children.Add(ReadingList = new ReadingListView(this));

            var wv = CreateWebView();
            AbsoluteLayout.SetLayoutBounds(wv, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(wv, AbsoluteLayoutFlags.All);
            MainContent.Children.Insert(MainContent.Children.IndexOf(WebViewPlaceholder), wv);
            MainContent.Children.Remove(WebViewPlaceholder);
            WebViewPlaceholder = null;

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

            ToolbarItems.Add(new ToolbarItem
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
                    ReadingList.AddAsync(Current.AbsoluteUri);
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

            ToolbarItems.Add(new ToolbarItem
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
                        urlEntry.Text = Current.AbsoluteUri;
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
            ToolbarItems.Add(new ToolbarItem
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
            App.Database.SaveVariable("Sleep:URI", Current.AbsoluteUri);
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
                    if (workchaps.TryGetValue(workid, out wc) && wc.chapterid != 0)
                    {
                        Navigate(new Uri(string.Concat("http://archiveofourown.org/works/", workid, "/chapters/", wc.chapterid, "#ao3t:jump")));
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

#if WINDOWS_UWP
        public Windows.Foundation.IAsyncOperation<IDictionary<long, WorkChapter>> GetWorkChaptersAsync(long[] works)
        {
            return App.Storage.getWorkChaptersAsync(works).AsAsyncOperation();
        }
#else
        public Task<IDictionary<long, WorkChapter>> GetWorkChaptersAsync(long[] works)
        {
            return App.Storage.getWorkChaptersAsync(works);
        }
#endif

        public bool IsMainThread
        {
#if WINDOWS_UWP
            get { return Dispatcher.HasThreadAccess; }
#elif __ANDROID__
            get { return Looper.MainLooper == Looper.MyLooper(); }
#endif
        }

        public T DoOnMainThread<T>(Func<T> function)
        {
            if (IsMainThread)
            {
                return function();
            }
            else
            {
                T result = default(T);
                var handle = new ManualResetEventSlim();

                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    result = function();
                    handle.Set();
                });
                handle.Wait();

                return result;
            }
        }
        public async Task<T> DoOnMainThreadAsync<T>(Func<T> function)
        {
            if (IsMainThread)
            {
                return function();
            }
            else
            {
                T result = default(T);
                var handle = new ManualResetEventSlim();

                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    result = function();
                    handle.Set();
                });
                await Task.Run(() => handle.Wait());

                return result;
            }
        }

        object IEventHandler.DoOnMainThread(MainThreadFunc function)
        {
            if (IsMainThread)
            {
                return function();
            }
            else
            {
                object result = null;
                var handle = new ManualResetEventSlim();

                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    result = function();
                    handle.Set();
                });
                handle.Wait();

                return result;
            }
        }

        public void DoOnMainThread(MainThreadAction function)
        {
            if (IsMainThread)
            {
                function();
            }
            else
            {
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    function();
                });
            }
        }

        public async Task DoOnMainThreadAsync(MainThreadAction function)
        {
            if (IsMainThread)
            {
                function();
            }
            else
            {
                var handle = new ManualResetEventSlim();
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    function();
                    handle.Set();
                });
                await Task.Run(() => handle.Wait());
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
                        nextPage = new Uri(Current, value);
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
                        prevPage = new Uri(Current, value);
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
                    if (currentLocation != null && currentLocation.workid == currentSavedLocation?.workid)
                    {
                        forceSetLocationButton.IsEnabled = currentLocation.IsNewer(currentSavedLocation);
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
                if (currentSavedLocation == null || currentSavedLocation.IsNewer(workchap))
                {
                    currentSavedLocation = workchap;
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                    {
                        lock (currentLocationLock)
                        {
                            if (currentLocation != null && currentLocation.workid == currentSavedLocation?.workid)
                                forceSetLocationButton.IsEnabled = currentLocation.IsNewer(currentSavedLocation);
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
                    var db_workchap = (await App.Storage.getWorkChaptersAsync(new[] { currentLocation.workid })).Select((kp)=>kp.Value).FirstOrDefault();
                    currentLocation = currentSavedLocation = new WorkChapter(currentLocation) { seq = (db_workchap?.seq ?? 0) + 1 };
                    App.Storage.setWorkChapters(new Dictionary<long, WorkChapter> { [currentLocation.workid] = currentSavedLocation });
                });
            }
        }
    }
}
