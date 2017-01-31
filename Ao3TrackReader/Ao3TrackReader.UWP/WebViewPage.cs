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
    public partial class WebViewPage : IEventHandler
    {
        const string JavaScriptInject = @"(function(){
            var head = document.getElementsByTagName('head')[0];
            for (var i = 0; i< Ao3TrackHelper.cssToInject.length; i++) {                    
                var link = document.createElement('link');
                link.type = 'text/css';
                link.rel = 'stylesheet';
                link.href = Ao3TrackHelper.cssToInject[i];
                head.appendChild(link);
            }
            for (var i = 0; i< Ao3TrackHelper.scriptsToInject.length; i++) {                    
                var script = document.createElement('script');
                script.type = 'text/javascript';
                script.src = Ao3TrackHelper.scriptsToInject[i];
                head.appendChild(script);
            }
        })();";

        public string[] scriptsToInject
        {
            get { return new[] {
                "ms-appx-web:///Content/platform.js",
                "ms-appx-web:///Content/reader.js",
                "ms-appx-web:///Content/ao3_tracker.js",
                "ms-appx-web:///Content/touch.js",
                "ms-appx-web:///Content/unitconv.js"
            }; }
        }
        public string[] cssToInject
        {
            get { return new[] { "ms-appx-web:///Content/ao3_tracker.css" }; }

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

            return WebView.ToView();
        }

        bool ShowBackOnToolbar
        {
            get
            {
                try {
                    Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
                    Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
                    return false;
                }
                catch (Exception)
                {

                }
                return true;
            }
        }

        private void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
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
            Title = "Loading...";

            var uri = Ao3SiteDataLookup.CheckUri(args.Uri);
            if (uri == null)
            {
                // Handle external uri
                args.Cancel = true;
                return;
            }
            else if (uri != args.Uri)
            {
                args.Cancel = true;
                WebView.Navigate(uri);
                return;
            }

            if (urlEntry != null) urlEntry.Text = args.Uri.AbsoluteUri;
            ReadingList?.PageChange(args.Uri);
            WebView.AddWebAllowedObject("Ao3TrackHelper", helper = new Ao3TrackHelper(this));
            nextPage = null;
            prevPage = null;
            prevPageButton.IsEnabled = canGoBack;
            nextPageButton.IsEnabled = canGoForward;
            currentLocation = null;
            currentSavedLocation = null;
            forceSetLocationButton.IsEnabled = false;
            helper?.Reset();
        }

        private void WebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
        {
            if (urlEntry != null) urlEntry.Text = WebView.Source.AbsoluteUri;
            ReadingList?.PageChange(Current);
            PrevPageIndicator.IsVisible = false;
            NextPageIndicator.IsVisible = false;
            WebView.RenderTransform = null;
            WebView.Opacity = 1;
            currentLocation = null;
            currentSavedLocation = null;
            forceSetLocationButton.IsEnabled = false;
            helper?.Reset();
        }

        Regex chapter_view_split_regex = new Regex(@"^(.*(?: - Chapter \d+)?) - ([^-]* - [^-]*)$");
        private void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            var t = WebView.DocumentTitle;

            t = t.EndsWith(" | Archive of Our Own") ? t.Substring(0, t.Length - 21) : t;
            t = t.EndsWith(" [Archive of Our Own]") ? t.Substring(0, t.Length - 21) : t;

            //t = chapter_view_split_regex.Replace(t,"$1\n$2", 1, 0);
            Title = t;

            // Inject JS script
            helper?.Reset();
            Task<string> task = WebView.InvokeScriptAsync("eval", new[] { JavaScriptInject }).AsTask();           
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

        public bool canGoBack { get { return WebView.CanGoBack || prevPage != null; } }

        public bool canGoForward { get { return WebView.CanGoForward || nextPage != null; } }

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

        public double leftOffset
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

        public double opacity
        {
            get
            {
                return WebView.Opacity;
            }
            set
            {
                WebView.Opacity = value;
            }
        }

        public Windows.Foundation.IAsyncOperation<string> showContextMenu(double x, double y, string[] menuItems)
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
