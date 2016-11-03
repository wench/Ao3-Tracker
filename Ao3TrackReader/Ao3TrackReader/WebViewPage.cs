using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Ao3TrackReader.Helper;
using System.Threading;

#if WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;
using SymbolIcon = Windows.UI.Xaml.Controls.SymbolIcon;
using Symbol = Windows.UI.Xaml.Controls.Symbol;
using AppBarButton = Windows.UI.Xaml.Controls.AppBarButton;
#endif


namespace Ao3TrackReader
{
    public partial class WebViewPage : ContentPage, IEventHandler
    {
#if WINDOWS_UWP
        AppBarButton jumpButton { get; set; }
        AppBarButton incFontSizeButton { get; set; }
        AppBarButton decFontSizeButton { get; set; }
        public Windows.UI.Core.CoreDispatcher Dispatcher { get; private set; }
#endif
        Label PrevPageIndicator;
        Label NextPageIndicator;

        public WebViewPage()
        {
            Title = "Ao3Track Reader";
            NavigationPage.SetHasNavigationBar(this, true);

            var layout = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Spacing = 0
            };

            Dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;

            var commandBar = CreateCommandBar();
#if WINDOWS_UWP
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Back", new SymbolIcon(Symbol.Back), true, () => { if (WebView.CanGoBack) WebView.GoBack(); }));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Forward", new SymbolIcon(Symbol.Forward), true, () => { if (WebView.CanGoForward) WebView.GoForward(); }));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Refresh", new SymbolIcon(Symbol.Refresh), true, () => WebView.Refresh()));
            commandBar.PrimaryCommands.Add(jumpButton = CreateAppBarButton("Jump", new SymbolIcon(Symbol.ShowBcc), false, this.OnJumpClicked));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Bookmarks", new SymbolIcon(Symbol.Bookmarks), true, () => { }));
            commandBar.PrimaryCommands.Add(incFontSizeButton = CreateAppBarButton("Font Increase", new SymbolIcon(Symbol.FontIncrease), true, () =>
                FontSize += 10));
            commandBar.PrimaryCommands.Add(decFontSizeButton = CreateAppBarButton("Font Decrease", new SymbolIcon(Symbol.FontDecrease), true, () =>
                FontSize -= 10));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Zoom In", new SymbolIcon(Symbol.ZoomIn), true, () => { }));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Zoom Out", new SymbolIcon(Symbol.ZoomOut), true, () => { }));

            commandBar.SecondaryCommands.Add(CreateAppBarButton("Close Page", new SymbolIcon(Symbol.Clear), true, () => { }));
            commandBar.SecondaryCommands.Add(CreateAppBarButton("Reset Font Size", new SymbolIcon(Symbol.Font), true, () => FontSize = 100));
            commandBar.SecondaryCommands.Add(CreateAppBarButton("Sync", new SymbolIcon(Symbol.Sync), true, () => { }));
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
            Navigate("https://archiveofourown.com/");

            // retore font size!
            FontSize = 100;

            layout.Children.Add(new AbsoluteLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = {
                        wv,
                        PrevPageIndicator,
                        NextPageIndicator
                    }
            });
            layout.Children.Add(commandBar);

            /*
            ToolbarItem tbi = null;

            tbi = new ToolbarItem("Jump", "jump.png", () =>
            {
                webView.OnJumpClicked();
            }, 0, 0);

            ToolbarItems.Add(tbi);
            */
            Content = layout;

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
        public Windows.Foundation.IAsyncOperation<IDictionary<long, IWorkChapter>> GetWorkChaptersAsync(long[] works)
        {
            return App.Storage.getWorkChaptersAsync(works);
        }
#endif

        public object DoOnMainThread(MainThreadFunc function)
        {

            if (Dispatcher.HasThreadAccess)
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
            if (Dispatcher.HasThreadAccess)
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
    }
}
