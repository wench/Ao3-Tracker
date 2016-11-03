using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ao3TrackReader.Helper;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Xamarin.Forms.Platform.UWP;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using System.Threading;

namespace Ao3TrackReader
{
    public partial class WebViewPage
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
            get { return new[] { "ms-appx-web:///Content/ao3_t_reader.js", "ms-appx-web:///Content/ao3_tracker.js" }; }
        }
        public string[] cssToInject
        {
            get { return new[] { "ms-appx-web:///Content/ao3_tracker.css" }; }

        }

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

            return WebView.ToView();
        }

        private CommandBar CreateCommandBar()
        {
            return new CommandBar
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
                ClosedDisplayMode = AppBarClosedDisplayMode.Compact,
                OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible
            };
        }

        private AppBarButton CreateAppBarButton(string label, IconElement icon, bool enabled, Action clicked)
        {

            var button = new AppBarButton
            {
                Icon = icon,
                Label = label,
                IsEnabled = enabled
            };
            button.Click += (sender, e) => { clicked(); };
            return button;

        }

        Ao3TrackHelper helper;

        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            jumpButton.IsEnabled = false;
            Title = "Loading...";
            WebView.AddWebAllowedObject("Ao3TrackHelper", helper = new Ao3TrackHelper(this));
        }

        private void WebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
        {
            PrevPageIndicator.IsVisible = false;
            NextPageIndicator.IsVisible = false;
            WebView.RenderTransform = null;
            WebView.Opacity = 1;
        }

        private void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            var t = WebView.DocumentTitle;
            Title = t.EndsWith(" | Archive of Our Own") ? t.Substring(0, t.Length - 21) : t;
                // Inject JS script
            Task<string> task = WebView.InvokeScriptAsync("eval", new[] { JavaScriptInject }).AsTask();           
        }


        void Navigate(string uri)
        {
            WebView.Navigate(new Uri(uri));
        }

        public bool canGoBack { get { return WebView.CanGoBack; } }

        public bool canGoForward { get { return WebView.CanGoForward; } }

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

        public double realWidth
        {
            get { return WebView.ActualWidth; }

        }
        public double realHeight
        {
            get { return WebView.ActualHeight; }
        }

    }
}
