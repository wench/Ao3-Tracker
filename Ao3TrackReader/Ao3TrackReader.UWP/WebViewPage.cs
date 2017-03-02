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
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Ao3TrackReader
{
    public partial class WebViewPage : IWebViewPage, IWebViewPageNative
    {
        public string CSSInject { get; } = @"(function(){
            var head = document.getElementsByTagName('head')[0];
            for (var i = 0; i< Ao3TrackHelperNative.cssToInject.length; i++) {                    
                var link = document.createElement('link');
                link.type = 'text/css';
                link.rel = 'stylesheet';
                link.href = Ao3TrackHelperNative.cssToInject[i];
                head.appendChild(link);
            }
            for (var i = 0; i< Ao3TrackHelperNative.scriptsToInject.length; i++) {                    
                var script = document.createElement('script');
                script.type = 'text/javascript';
                script.src = Ao3TrackHelperNative.scriptsToInject[i];
                head.appendChild(script);
            }
        })();";

        public string[] ScriptsToInject { get; } = new[] {
                "ms-appx-web:///Content/marshal.js",
                "ms-appx-web:///Content/platform.js",
                "ms-appx-web:///Content/reader.js",
                "ms-appx-web:///Content/tracker.js",
                "ms-appx-web:///Content/touch.js",
                "ms-appx-web:///Content/unitconv.js"
        };
        public string[] CssToInject { get; } = new[] { "ms-appx-web:///Content/tracker.css" }; 

        private CoreDispatcher Dispatcher { get; set; }

        public bool IsMainThread
        {
            get { return Dispatcher.HasThreadAccess; }
        }

        WebView WebView { get; set; }

        public Xamarin.Forms.View CreateWebView()
        {
            Dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

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
            helper = new Ao3TrackHelper(this);

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
            OnWebViewGotFocus();
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
            args.Cancel = OnNavigationStarting(args.Uri);
            if (!args.Cancel)
                AddJavascriptObject("Ao3TrackHelperNative", helper);
        }

        private void WebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
        {
            OnContentLoading();
        }

        private void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            OnContentLoaded();
        }

        public async Task<string> EvaluateJavascriptAsync(string code)
        {
            return await WebView.InvokeScriptAsync("eval", new[] { code });
        }

        public void AddJavascriptObject(string name, object obj)
        {
            WebView.AddWebAllowedObject(name, obj);
        }

        async void InjectScripts()
        {
            await EvaluateJavascriptAsync(CSSInject);
        }

        public Uri CurrentUri
        {
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

        public double DeviceWidth
        {
            get
            {
                return WebView.ActualWidth;
            }
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
        
        TaskCompletionSource<string> contextMenuResult = null;
        MenuFlyout contextMenu = null;
        public void HideContextMenu()
        {
            if (contextMenuResult != null)
            {
                contextMenuResult.TrySetCanceled();
                contextMenuResult = null;
            }
            if (contextMenu != null)
            {
                contextMenu.Hide();
                contextMenu = null;
            }
        }

        public Task<string> ShowContextMenu(double x, double y, string[] menuItems)
        {
            HideContextMenu();

            contextMenuResult = new TaskCompletionSource<string>();
            RoutedEventHandler clicked = (sender, e) =>
            {
                var item = sender as MenuFlyoutItem;
                contextMenuResult?.TrySetResult(item.Text);
            };
            contextMenu = new MenuFlyout();
            for (int i = 0; i < menuItems.Length; i++) {
                if (menuItems[i] == "-") {
                    contextMenu.Items.Add(new MenuFlyoutSeparator());
                }
                else
                {
                    var item = new MenuFlyoutItem { Text = menuItems[i] };
                    item.Click += clicked;
                    contextMenu.Items.Add(item);
                }
            }

            contextMenu.Closed += (sender,e) =>
            {
                contextMenuResult?.TrySetResult("");
            };

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => contextMenu.ShowAt(WebView, new Windows.Foundation.Point(x, y)));

            return contextMenuResult.Task.ContinueWith((task)=> {
                contextMenu = null;
                contextMenuResult = null;
                return task.Result;
            });

        }

        IAsyncOperation<IDictionary<long, WorkChapter>> IWebViewPage.GetWorkChaptersAsync(long[] works)
        {
            return GetWorkChaptersAsync(works).AsAsyncOperation();
        }
        IAsyncOperation<IDictionary<string, bool>> IWebViewPage.AreUrlsInReadingListAsync(string[] urls)
        {
            return AreUrlsInReadingListAsync(urls).AsAsyncOperation();
        }
        IAsyncOperation<string> Helper.IWebViewPage.CallJavascriptAsync(string function, params object[] args)
        {
            return CallJavascriptAsync(function, args).AsAsyncOperation();
        }
        IAsyncOperation<string> Helper.IWebViewPage.EvaluateJavascriptAsync(string code)
        {
            return CallJavascriptAsync(code).AsAsyncOperation();
        }
        IAsyncOperation<string> Helper.IWebViewPage.ShowContextMenu(double x, double y, string[] menuItems)
        {
            return ShowContextMenu(x, y, menuItems).AsAsyncOperation();
        }
    }
}
