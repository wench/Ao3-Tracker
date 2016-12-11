﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Ao3TrackReader.Helper;
using System.Threading;
using System.Text.RegularExpressions;

#if WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;
using SymbolIcon = Windows.UI.Xaml.Controls.SymbolIcon;
using Symbol = Windows.UI.Xaml.Controls.Symbol;
using AppBarButton = Windows.UI.Xaml.Controls.AppBarButton;
#elif __ANDROID__
using Android.OS;
#endif


namespace Ao3TrackReader
{
    public partial class WebViewPage : ContentPage, IEventHandler
    {
#if WINDOWS_UWP
        AppBarButton jumpButton { get; set; }
        AppBarButton incFontSizeButton { get; set; }
        AppBarButton decFontSizeButton { get; set; }
        AppBarButton nextPageButton { get; set; }
        AppBarButton prevPageButton { get; set; }
        public Windows.UI.Core.CoreDispatcher Dispatcher { get; private set; }
#endif
        Label PrevPageIndicator;
        Label NextPageIndicator;
        Controls.ReadingListView readingList;
        Entry urlEntry;
        StackLayout urlBar;
        AbsoluteLayout modelPopup;

        public WebViewPage()
        {
            Title = "Ao3Track Reader";

            var mainlayout = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Spacing = 0
            };

            var commandBar = CreateCommandBar();
#if WINDOWS_UWP
            Dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;
            commandBar.PrimaryCommands.Add(prevPageButton = CreateAppBarButton("Back", new SymbolIcon(Symbol.Back), false, () => { GoBack(); }));
            commandBar.PrimaryCommands.Add(nextPageButton = CreateAppBarButton("Forward", new SymbolIcon(Symbol.Forward), false, () => { GoForward(); }));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Refresh", new SymbolIcon(Symbol.Refresh), true, () => WebView.Refresh()));
            commandBar.PrimaryCommands.Add(jumpButton = CreateAppBarButton("Jump", new SymbolIcon(Symbol.Redo), false, this.OnJumpClicked));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Reading List", new SymbolIcon(Symbol.Bookmarks), true, () => readingList.IsOnScreen = !readingList.IsOnScreen ));
            commandBar.PrimaryCommands.Add(incFontSizeButton = CreateAppBarButton("Font Increase", new SymbolIcon(Symbol.FontIncrease), true, () =>
                FontSize += 10));
            commandBar.PrimaryCommands.Add(decFontSizeButton = CreateAppBarButton("Font Decrease", new SymbolIcon(Symbol.FontDecrease), true, () =>
                FontSize -= 10));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Sync", new SymbolIcon(Symbol.Sync), true, () => { }));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Url Bar", new SymbolIcon(Symbol.Rename), true, () =>
            {
                if (urlBar.IsVisible)
                {
                    urlBar.IsVisible = false;
                    urlBar.Unfocus();
                }
                else
                {
                    urlEntry.Text = WebView.Source.AbsoluteUri;
                    urlBar.IsVisible = true;
                    urlEntry.Focus();
                }
            }));

            commandBar.SecondaryCommands.Add(CreateAppBarButton("Reset Font Size", new SymbolIcon(Symbol.Font), true, () => FontSize = 100));
            commandBar.SecondaryCommands.Add(CreateAppBarButton("Settings", new SymbolIcon(Symbol.Setting), true, SettingsButton_Clicked));

#else
#endif
            NextPageIndicator = new Label { Text = "Next Page", Rotation = 90, VerticalTextAlignment = TextAlignment.Start, HorizontalTextAlignment = TextAlignment.Center, IsVisible = false };
            AbsoluteLayout.SetLayoutBounds(NextPageIndicator, new Rectangle(.98, .5, 100, 100));
            AbsoluteLayout.SetLayoutFlags(NextPageIndicator, AbsoluteLayoutFlags.PositionProportional);

            PrevPageIndicator = new Label { Text = "Previous Page", Rotation = 270, VerticalTextAlignment = TextAlignment.Start, HorizontalTextAlignment = TextAlignment.Center, IsVisible = false };
            AbsoluteLayout.SetLayoutBounds(PrevPageIndicator, new Rectangle(.02, .5, 100, 100));
            AbsoluteLayout.SetLayoutFlags(PrevPageIndicator, AbsoluteLayoutFlags.PositionProportional);

            var wv = CreateWebView();
            AbsoluteLayout.SetLayoutBounds(wv, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(wv, AbsoluteLayoutFlags.All);
            Navigate(new Uri("https://archiveofourown.com/"));

            // retore font size!
            FontSize = 100;

            readingList = new Controls.ReadingListView(this);
            AbsoluteLayout.SetLayoutBounds(readingList, new Rectangle(1, 0, 480, 1));
            AbsoluteLayout.SetLayoutFlags(readingList, AbsoluteLayoutFlags.HeightProportional | AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.YProportional);

            urlBar = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Spacing = 4
            };
            urlEntry = new Entry
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            urlEntry.Completed += UrlButton_Clicked;
            urlEntry.Keyboard = Keyboard.Url;

            urlBar.Children.Add(urlEntry);

            var urlButton = new Button()
            {
                //Text = "\x2192",
                Text = "Go",
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };
            urlButton.Clicked += UrlButton_Clicked;

            var urlCancel = new Button()
            {
                //Text = "\xD7",
                Text = "\x274C",
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };
            urlCancel.Clicked += UrlCancel_Clicked;

            urlBar.Children.Add(urlEntry);
            urlBar.Children.Add(urlButton);
            urlBar.Children.Add(urlCancel);
            urlBar.BackgroundColor = Color.Black;
            urlBar.IsVisible = false;

            mainlayout.Children.Add(new AbsoluteLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = {
                        wv,
                        PrevPageIndicator,
                        NextPageIndicator,
                        readingList,
                    }
            });
            mainlayout.Children.Add(urlBar);
            mainlayout.Children.Add(commandBar);
            mainlayout.SizeChanged += Mainlayout_SizeChanged;

            /*
            ToolbarItem tbi = null;

            tbi = new ToolbarItem("Jump", "jump.png", () =>
            {
                webView.OnJumpClicked();
            }, 0, 0);

            ToolbarItems.Add(tbi);
            */
            AbsoluteLayout.SetLayoutBounds(mainlayout, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(mainlayout, AbsoluteLayoutFlags.All);

            modelPopup = new AbsoluteLayout();
            AbsoluteLayout.SetLayoutBounds(modelPopup, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(modelPopup, AbsoluteLayoutFlags.All);
            //modelPopup.BackgroundColor = new Color(0, 0, 0, .5);

            var outerlayout = new AbsoluteLayout
            {
                Children =
                {
                    mainlayout,
                    modelPopup

                }

            };

            Content = outerlayout;
        }

        private void Mainlayout_SizeChanged(object sender, EventArgs e)
        {
            var mainlayout = sender as Layout;
            double desired;

            if (mainlayout.Width < 480)
                AbsoluteLayout.SetLayoutBounds(readingList, new Rectangle(1, 0, desired = mainlayout.Width, 1));
            else if (mainlayout.Width < 960)
                AbsoluteLayout.SetLayoutBounds(readingList, new Rectangle(1, 0, desired = 480, 1));
            else
                AbsoluteLayout.SetLayoutBounds(readingList, new Rectangle(1, 0, desired = mainlayout.Width/2, 1));
        }

        public void NavigateToLast(long workid)
        {
            Task.Run(async () =>
            {
                var workchaps = await App.Storage.getWorkChaptersAsync(new[] { workid });

                DoOnMainThread(() => {
                    IWorkChapter wc;
                    if (workchaps.TryGetValue(workid, out wc) && wc.chapterid != 0)
                    {
                        Navigate(new Uri(string.Concat("https://archiveofourown.org/works/", workid, "/chapters/", wc.chapterid, "#ao3t:jump")));
                    }
                    else
                    {
                        Navigate(new Uri(string.Concat("https://archiveofourown.org/works/", workid, "#ao3t:jump")));
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
                var uri = new UriBuilder(urlEntry.Text);
                if (uri.Host == "archiveofourown.org" || uri.Host == "www.archiveofourown.org")
                {
                    if (uri.Scheme == "http")
                    {
                        uri.Scheme = "https";
                    }
                    uri.Port = -1;
                    WebView.Navigate(uri.Uri);
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

        public int FontSizeMax { get { return 300; } }
        public int FontSizeMin { get { return 10; } }
        private int font_size = 100;
        public int FontSize
        {
            get { return font_size; }
            set
            {
                font_size = value;
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    decFontSizeButton.IsEnabled = FontSize > FontSizeMin;
                    incFontSizeButton.IsEnabled = FontSize < FontSizeMax;
                });
                helper.OnAlterFontSizeEvent();
            }
        }


        static object locker = new object();

#if WINDOWS_UWP
        public Windows.Foundation.IAsyncOperation<IDictionary<long, IWorkChapter>> GetWorkChaptersAsync(long[] works)
        {
            return App.Storage.getWorkChaptersAsync(works).AsAsyncOperation();
        }
#else
        public Task<IDictionary<long, IWorkChapter>> GetWorkChaptersAsync(long[] works)
        {
            return App.Storage.getWorkChaptersAsync(works);
        }
#endif

        public bool IsMainThread {
#if WINDOWS_UWP
            get { return Dispatcher.HasThreadAccess; }
#elif __ANDROID__
            get { return Looper.MainLooper != Looper.MyLooper(); }
#endif
        }

        public object DoOnMainThread(MainThreadFunc function)
        {
            if (IsMainThread)
            {
                return function();
            }
            else
            {
                object result = null;
                ManualResetEventSlim handle = new ManualResetEventSlim();

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


        public void SetWorkChapters(IDictionary<long, IWorkChapter> works)
        {
            App.Storage.setWorkChapters(works);
        }

        public void OnJumpClicked()
        {
            Task.Run(() =>
            {
                helper.OnJumpToLastLocation(false);
            });
        }

        public bool JumpToLastLocationEnabled
        {
            set
            {
#if WINDOWS_UWP
                if (jumpButton != null) jumpButton.IsEnabled = value;
#else                
#endif
            }
            get
            {
#if WINDOWS_UWP
                return jumpButton?.IsEnabled ?? false;
#else
#endif              
            }
        }

        public async void SettingsButton_Clicked()
        {
            var settingsPage = new SettingsPage();

            await Navigation.PushModalAsync(settingsPage);
        }

        public bool showPrevPageIndicator
        {
            get { return PrevPageIndicator.IsVisible; }
            set
            {
                PrevPageIndicator.IsVisible = value;
            }
        }
        public bool showNextPageIndicator
        {
            get { return NextPageIndicator.IsVisible; }
            set
            {
                NextPageIndicator.IsVisible = value;
            }
        }


        public void addToReadingList(string href)
        {
            readingList.AddAsync(href);
        }

        public void setCookies(string cookies)
        {
            if (App.Database.GetVariable("siteCookies") != cookies)
                App.Database.SaveVariable("siteCookies", cookies);
        }
    }
}
