﻿using System;
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
            WebView.NewWindowRequested += WebView_NewWindowRequested;
            WebView.GotFocus += WebView_GotFocus;

            return WebView.ToView();
        }

        private void WebView_GotFocus(object sender, RoutedEventArgs e)
        {
            readingList.IsOnScreen = false;
        }

        private void WebView_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            if (args.Uri.Host == "archiveofourown.org" || args.Uri.Host == "www.archiveofourown.org") {
                var uri = new UriBuilder(args.Uri);
                if (uri.Scheme == "http")
                    uri.Scheme = "https";
                uri.Port = -1;
                WebView.Navigate(uri.Uri);
                args.Handled = true;
            }
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

            if (args.Uri.Host == "archiveofourown.org" || args.Uri.Host == "www.archiveofourown.org")
            {
                if (args.Uri.Scheme == "http") {
                    var uri = new UriBuilder(args.Uri);
                    uri.Scheme = "https";
                    uri.Port = -1;
                    args.Cancel = true;
                    WebView.Navigate(uri.Uri);
                    return;
                }

            }
            if (urlEntry != null) urlEntry.Text = args.Uri.AbsoluteUri;
            WebView.AddWebAllowedObject("Ao3TrackHelper", helper = new Ao3TrackHelper(this));
            nextPage = null;
            prevPage = null;
            prevPageButton.IsEnabled = canGoBack;
            nextPageButton.IsEnabled = canGoForward;
            helper?.ClearEvents();
        }

        private void WebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
        {
            if (urlEntry != null) urlEntry.Text = WebView.Source.AbsoluteUri;
            PrevPageIndicator.IsVisible = false;
            NextPageIndicator.IsVisible = false;
            WebView.RenderTransform = null;
            WebView.Opacity = 1;
            helper?.ClearEvents();
        }

        private void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            var t = WebView.DocumentTitle;

            t = t.EndsWith(" | Archive of Our Own") ? t.Substring(0, t.Length - 21) : t;
            t = t.EndsWith(" [Archive of Our Own]") ? t.Substring(0, t.Length - 21) : t;
            Title = t;

            // Inject JS script
            helper?.ClearEvents();
            Task<string> task = WebView.InvokeScriptAsync("eval", new[] { JavaScriptInject }).AsTask();           
        }

        public Uri Current {
            get { return WebView.Source; }
        }

        public void Navigate(Uri uri)
        {
            helper?.ClearEvents();
            WebView.Navigate(uri);
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
                        nextPage = new Uri(WebView.Source, value);
                    }
                    catch
                    {

                    }
                }
                nextPageButton.IsEnabled = canGoForward;
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
                        prevPage = new Uri(WebView.Source, value);
                    }
                    catch
                    {
                    }
                }
                prevPageButton.IsEnabled = canGoBack;
            }
        }

        public bool canGoBack { get { return WebView.CanGoBack || prevPage != null; } }

        public bool canGoForward { get { return WebView.CanGoForward || nextPage != null; } }

        public void GoBack()
        {
            if (WebView.CanGoBack) WebView.GoBack();
            else if (prevPage != null) WebView.Navigate(prevPage);
        }
        public void GoForward()
        {
            if (WebView.CanGoForward) WebView.GoForward();
            else if (nextPage != null) WebView.Navigate(nextPage);

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
