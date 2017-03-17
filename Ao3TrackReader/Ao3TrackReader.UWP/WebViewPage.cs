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
using Windows.ApplicationModel.DataTransfer;

namespace Ao3TrackReader
{
    public partial class WebViewPage : IWebViewPage, IWebViewPageNative
    {
        public string[] ScriptsToInject { get; } = new[] {
                "jquery-3.1.1.js",
                "marshal.js",
                "platform.js",
                "reader.js",
                "tracker.js",
                "touch.js",
                "unitconv.js"
        };
        public string[] CssToInject { get; } = new[] { "tracker.css" };

        private CoreDispatcher Dispatcher;

        public bool IsMainThread
        {
            get { return Dispatcher.HasThreadAccess; }
        }

        WebView webView;

        public Xamarin.Forms.View CreateWebView()
        {
            Dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            webView = new WebView(WebViewExecutionMode.SeparateThread)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            webView.NavigationStarting += WebView_NavigationStarting;
            webView.DOMContentLoaded += WebView_DOMContentLoaded;
            webView.ContentLoading += WebView_ContentLoading;
            webView.NewWindowRequested += WebView_NewWindowRequested;
            webView.GotFocus += WebView_GotFocus;
            webView.DefaultBackgroundColor = Ao3TrackReader.Resources.Colors.Alt.MediumHigh.ToWindows();
            helper = new Ao3TrackHelper(this);

            contextMenu = new MenuFlyout();
            foreach (var kvp in ContextMenuItems)
            {
                if (kvp.Key == "-")
                {
                    contextMenu.Items.Add(new MenuFlyoutSeparator());
                }
                else
                {
                    var item = new MenuFlyoutItem { Text = kvp.Key };
                    item.Command = kvp.Value;
                    contextMenu.Items.Add(item);
                }
            }

            return webView.ToView();
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
                webView.Navigate(uri);
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
            return await webView.InvokeScriptAsync("eval", new[] { code });
        }

        public void AddJavascriptObject(string name, object obj)
        {
            webView.AddWebAllowedObject(name, obj);
        }

        Task OnInjectingScripts(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        Task OnInjectedScripts(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        async Task<string> ReadFile(string name, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Content/" + name));

            ct.ThrowIfCancellationRequested();
            return await Windows.Storage.FileIO.ReadTextAsync(file);
        }

        public Uri CurrentUri
        {
            get {
                return DoOnMainThread(() => {
                    return webView.Source;
                });
            }
        }

        public void Navigate(Uri uri)
        {
            uri = Ao3SiteDataLookup.CheckUri(uri);
            if (uri == null) return;
            helper?.Reset();
            webView.Navigate(uri);
        }

        public void Refresh()
        {
            webView.Refresh();
        }

        bool WebViewCanGoBack => webView.CanGoBack;

        bool WebViewCanGoForward => webView.CanGoForward;

        void WebViewGoBack()
        {
            webView.GoBack();
        }
        void WebViewGoForward()
        {
            webView.GoForward();
        }

        public double DeviceWidth
        {
            get
            {
                return webView.ActualWidth;
            }
        }

        public double LeftOffset
        {
            get
            {
                    TranslateTransform v = webView.RenderTransform as TranslateTransform;
                    return v?.X ?? 0.0;
            }
            set
            {
                    if (value == 0.0)
                    {
                        webView.RenderTransform = null;
                    }
                    else
                    {
                        webView.RenderTransform = new TranslateTransform { X = value, Y = 0.0 };
                    }
            }
        }

        public void CopyToClipboard(string str, string type)
        {
            if (type == "text")
            {
                var dp = new DataPackage();
                dp.SetText(str);
                Clipboard.SetContent(dp);
            }
            else if (type == "url")
            {
                var dp = new DataPackage();
                dp.SetText(str);
                dp.SetWebLink(new Uri(str));
                Clipboard.SetContent(dp);
            }
        }

        MenuFlyout contextMenu;
        public void HideContextMenu()
        {
            contextMenu.Hide();
        }

        public async void ShowContextMenu(double x, double y, string url, string innerHtml)
        {
            HideContextMenu();

            var res = await AreUrlsInReadingListAsync(new[] { url });
            ContextMenuOpenAdd.IsEnabled = !res[url];
            ContextMenuAdd.IsEnabled = !res[url];

            foreach (var baseitem in contextMenu.Items)
            {
                if (baseitem is MenuFlyoutItem item)
                {
                    item.CommandParameter = url;
                }
            }

            contextMenu.ShowAt(webView, new Windows.Foundation.Point(x, y));
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

    }
}
