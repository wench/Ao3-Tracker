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

namespace Ao3TrackReader
{
    public partial class WebViewPage
    {
        const string JavaScriptInject = @"(function(){
            var head = document.getElementsByTagName('head')[0];
            ['ms-appx-web:///Content/ao3_t_reader.js', 'ms-appx-web:///Content/ao3_tracker.js'].forEach(function(uri){            
                var script = document.createElement('script');
                script.type = 'text/javascript';
                script.src = uri;
                head.appendChild(script);
            });
            ['ms-appx-web:///Content/ao3_tracker.css'].forEach(function(uri){            
                var link = document.createElement('link');
                link.type = 'text/css';
                link.rel = 'stylesheet';
                link.href = uri;
                head.appendChild(link);
            });
        })();";

        WebView WebView { get; set; }
        AppBarButton jumpButton { get; set; }

        void EnableInjection()
        {
        }

        Ao3TrackHelper helper;

        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            EnableJumpToLastLocation(false);
            helper = new Ao3TrackHelper(this);
            WebView.AddWebAllowedObject("Ao3TrackHelper", helper);
            Title = "Loading...";
        }

        private void WebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
        {
            leftOffset = 0;
            opacity = 1.0;
        }

        private async void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            var t = WebView.DocumentTitle;
            Title = t.EndsWith(" | Archive of Our Own") ? t.Substring(0, t.Length-21) : t;
            // Inject JS script
            await WebView.InvokeScriptAsync("eval", new[] { JavaScriptInject });
        }

        private Xamarin.Forms.View CreateWebView()
        {
            WebView = new WebView(WebViewExecutionMode.SeparateThread)
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

        void Navigate(string uri)
        {
            WebView.Navigate(new Uri(uri));
        }

        async Task<T> DoCallbackAsync<T>(Func<T> function) where T: new()
        {
            T result = new T();
            await WebView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                result = function();
            });
            return result;
        }

        Task DoCallbackAsync(Action function)
        {
            return WebView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                function();
            }).AsTask();
        }

        public bool canGoBack { get { return DoCallbackAsync(() => WebView.CanGoBack).Result; } }

        public bool canGoForward { get { return DoCallbackAsync(() => WebView.CanGoForward).Result; } }

        public double leftOffset
        {
            get
            {
                return DoCallbackAsync(() => {
                    TranslateTransform v = WebView.RenderTransform as TranslateTransform;
                    return v?.X ?? 0.0;
                }).Result;
            }
            set
            {
                DoCallbackAsync(() =>
                {
                    if (value == 0.0)
                    {
                        WebView.RenderTransform = null;
                    }
                    else
                    {
                        WebView.RenderTransform = new TranslateTransform { X = value, Y = 0.0 };
                    }
                });
            }
        }

        public double opacity
        {
            get
            {
                return DoCallbackAsync(() => WebView.Opacity).Result;
            }
            set
            {
                DoCallbackAsync(() => { WebView.Opacity = value; });
            }
        }
    }
}
