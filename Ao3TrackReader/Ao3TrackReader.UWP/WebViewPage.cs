using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ao3TrackReader.Helper;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Xamarin.Forms.Platform.UWP;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using System.Threading;
using Ao3TrackReader.Data;
using Windows.Foundation.Metadata;

namespace Ao3TrackReader
{
    public partial class WebViewPage : IWebViewPage
    {
        const string JavaScriptInject = @"(function(){
            var head = document.getElementsByTagName('head')[0];
            for (var i = 0; i< Ao3TrackHelperUWP.cssToInject.length; i++) {                    
                var link = document.createElement('link');
                link.type = 'text/css';
                link.rel = 'stylesheet';
                link.href = Ao3TrackHelperUWP.cssToInject[i];
                head.appendChild(link);
            }
            for (var i = 0; i< Ao3TrackHelperUWP.scriptsToInject.length; i++) {                    
                var script = document.createElement('script');
                script.type = 'text/javascript';
                script.src = Ao3TrackHelperUWP.scriptsToInject[i];
                head.appendChild(script);
            }
        })();";

        public string[] ScriptsToInject
        {
            get { return new[] {
                "ms-appx-web:///Content/platform.js",
                "ms-appx-web:///Content/reader.js",
                "ms-appx-web:///Content/tracker.js",
                "ms-appx-web:///Content/touch.js",
                "ms-appx-web:///Content/unitconv.js"
            }; }
        }
        public string[] CssToInject
        {
            get { return new[] { "ms-appx-web:///Content/tracker.css" }; }

        }

        Ao3TrackHelper helper;

        WebView WebView { get; set; }

        private Xamarin.Forms.View CreateWebView()
        {
            WebView = new WebView()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            WebView.NavigationStarting += WebView_NavigationStarting;
            WebView.DOMContentLoaded += WebView_DOMContentLoaded;
            WebView.ContentLoading += WebView_ContentLoading;
            WebView.NewWindowRequested += WebView_NewWindowRequested;
            WebView.GotFocus += WebView_GotFocus;
            WebView.DefaultBackgroundColor = Ao3TrackReader.Resources.Colors.Alt.MediumHigh.ToWindows();

            return WebView.ToView();
        }

        public bool ShowBackOnToolbar
        {
            get
            {
                try {
                    if (Windows.Foundation.Metadata.ApiInformation.IsEventPresent("Windows.Phone.UI.Input.HardwareButtons", "BackPressed"))
                    {
                        var eh = new EventHandler<Windows.Phone.UI.Input.BackPressedEventArgs>((sender, e) => { });
                        Windows.Phone.UI.Input.HardwareButtons.BackPressed += eh;
                        Windows.Phone.UI.Input.HardwareButtons.BackPressed -= eh;
                        return false;
                    }
                }
                catch (Exception)
                {

                }
                return true;
            }
        }

        private void WebView_GotFocus(object sender, RoutedEventArgs e)
        {
            ReadingList.IsOnScreen = false;
            SettingsPane.IsOnScreen = false;
        }

        private void WebView_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            var uri = Ao3SiteDataLookup.CheckUri(args.Uri);
            if (uri != null) {
                args.Handled = true;
                WebView.Navigate(uri);
            }
        }

        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            jumpButton.IsEnabled = false;

            var uri = Ao3SiteDataLookup.CheckUri(args.Uri);
            if (uri == null)
            {
                // Handle external uri
                args.Cancel = true;
                LeftOffset = 0;
                return;
            }
            else if (uri != args.Uri)
            {
                args.Cancel = true;
                WebView.Navigate(uri);
                return;
            }

            TitleEx = "Loading...";

            if (urlEntry != null) urlEntry.Text = args.Uri.AbsoluteUri;
            ReadingList?.PageChange(args.Uri);
            WebView.AddWebAllowedObject("Ao3TrackHelperUWP", helper = new Ao3TrackHelper(this));
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
        }

        private void WebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
        {
            if (urlEntry != null) urlEntry.Text = WebView.Source.AbsoluteUri;
            ReadingList?.PageChange(Current);
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

        private void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            // Inject JS script
            WebView.InvokeScriptAsync("eval", new[] { JavaScriptInject }).AsTask();
            LeftOffset = 0;
        }

        public Uri Current {
            get {
                return DoOnMainThread(()=> {
                    return WebView.Source;
                    });
            }
        }

        public void Navigate(Uri uri)
        {
            uri = Ao3SiteDataLookup.CheckUri(uri);
            if (uri == null) return;
            helper?.Reset();
            WebView.Navigate(uri);
        }

        public void Refresh()
        {
            WebView.Refresh();
        }

        public bool CanGoBack { get { return WebView.CanGoBack || prevPage != null; } }

        public bool CanGoForward { get { return WebView.CanGoForward || nextPage != null; } }

        public void GoBack()
        {
            if (WebView.CanGoBack) WebView.GoBack();
            else if (prevPage != null) Navigate(prevPage);
        }
        public void GoForward()
        {
            if (WebView.CanGoForward) WebView.GoForward();
            else if (nextPage != null) Navigate(nextPage);

        }

        public double LeftOffset
        {
            get
            {
                    TranslateTransform v = WebView.RenderTransform as TranslateTransform;
                    return v?.X ?? 0.0;
            }
            set
            {
                    if (value == 0.0)
                    {
                        WebView.RenderTransform = null;
                    }
                    else
                    {
                        WebView.RenderTransform = new TranslateTransform { X = value, Y = 0.0 };
                    }
            }
        }

        public Windows.Foundation.IAsyncOperation<string> ShowContextMenu(double x, double y, string[] menuItems)
        {
            string result = null;
            RoutedEventHandler clicked = (sender, e) =>
            {
                var item = sender as MenuFlyoutItem;
                result = item.Text;
            };
            MenuFlyout menu = new MenuFlyout();
            for (int i = 0; i < menuItems.Length; i++) {
                if (menuItems[i] == "-") {
                    menu.Items.Add(new MenuFlyoutSeparator());
                }
                else
                {
                    var item = new MenuFlyoutItem { Text = menuItems[i] };
                    item.Click += clicked;
                    menu.Items.Add(item);
                }
            }

            ManualResetEventSlim handle = new ManualResetEventSlim();
            EventHandler<object> closed = (sender, e) =>
            {
                handle.Set();
            };
            menu.Closed += closed;

            menu.ShowAt(WebView, new Windows.Foundation.Point(x, y));
            return Task.Run(() => {
                handle.Wait();
                return result;
            }).AsAsyncOperation();

        }
    }
}
