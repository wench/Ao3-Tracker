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
#if WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;
#else
using Xamarin.Forms.Platform.WinRT;
#endif
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
        private CoreDispatcher Dispatcher { get; } = Window.Current.Dispatcher;

        public bool IsMainThread => Dispatcher.HasThreadAccess;

        WebView webView;

        public Xamarin.Forms.View CreateWebView()
        {
#if WINDOWS_UWP
            webView = new WebView(WebViewExecutionMode.SameThread)
#else
            webView = new WebView()
#endif
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            webView.NavigationStarting += WebView_NavigationStarting;
            webView.DOMContentLoaded += WebView_DOMContentLoaded;
            webView.ContentLoading += WebView_ContentLoading;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.GotFocus += WebView_GotFocus;
            webView.DefaultBackgroundColor = Ao3TrackReader.Resources.Colors.Alt.MediumHigh.ToWindows();
            webView.RegisterPropertyChangedCallback(WebView.DocumentTitleProperty, webViewDocumentTitleChanged);

            CreateWebViewAdditional();

            contextMenu = new MenuFlyout();
            foreach (var kvp in ContextMenuItems)
            {
                if (kvp.Key == "-")
                {
                    contextMenu.Items.Add(new WinRT.MenuFlyoutSeparatorEx { Command = kvp.Value });
                }
                else
                {
                    var item = new MenuFlyoutItem { Text = kvp.Key, Command = kvp.Value };
                    //item.Foreground = new SolidColorBrush(Ao3TrackReader.Resources.Colors.Base.MediumHigh.ToWindows());
                    contextMenu.Items.Add(item);
                }
            }

            return webView.ToView();
        }


        public void ShowErrorPage(string message, Uri uri)
        {
            var html = GetErrorPageHtml(message, uri);
            webView.NavigateToString(html);
        }

        private void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (!args.IsSuccess)
            {
                var reason = args.WebErrorStatus.ToString("d") + " " + string.Join(" ", Regex.Split(args.WebErrorStatus.ToString(), @"(?<!^)(?=[A-Z](?![A-Z]|$))"));
                ShowErrorPage(reason, args.Uri);
            }
        }

        private void WebView_GotFocus(object sender, RoutedEventArgs e)
        {
            OnWebViewGotFocus();
        }

        bool doLoading = false;
        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            args.Cancel = OnNavigationStarting(args.Uri);
            if (!args.Cancel)
            {
                AddJavascriptObject("Ao3TrackHelperNative", helper);
                doLoading = true;
            }
        }

        private void webViewDocumentTitleChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (doLoading && !string.IsNullOrWhiteSpace(webView.DocumentTitle))
            {
                doLoading = false;
                DoOnMainThreadAsync(() => OnContentLoading()).ConfigureAwait(false);
            }
        }

        private void WebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
        {
            if (doLoading)
            {
                doLoading = false;
                DoOnMainThreadAsync(() => OnContentLoading()).ConfigureAwait(false);
            }
        }

        private void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            OnContentLoaded();
        }

        public async Task<string> EvaluateJavascriptAsync(string code)
        {
            try
            {
                return await webView.InvokeScriptAsync("eval", new[] { code });
            }
            catch (Exception e)
            {
                if (e.HResult != unchecked((int)0x80020101)) // happens sometimes
                    throw;
                else
                    return "";
            }
        }

        async Task<string> ReadFile(string name, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var uri = new Uri("ms-appx:///Content/" + name);
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);

            ct.ThrowIfCancellationRequested();
            return await Windows.Storage.FileIO.ReadTextAsync(file);
        }

        public Uri CurrentUri
        {
            get
            {
                return DoOnMainThreadAsync(() =>
                {
                    return webView.Source ?? new Uri("error:///");
                }).WaitGetResult();
            }
        }

        public void Navigate(Uri uri, bool allowext = true)
        {
            if (uri == null)
                return;

            var newuri = Ao3SiteDataLookup.CheckUri(uri);
            if (newuri == null)
            {
                if (allowext) OpenExternal(uri);
                return;
            }

            helper?.Reset();
            webView.Navigate(newuri);
        }

        public void Refresh()
        {
            webView.Refresh();
        }

        bool WebViewCanGoBack => DoOnMainThreadAsync(() => webView.CanGoBack).Result;

        bool WebViewCanGoForward => DoOnMainThreadAsync(() => webView.CanGoForward).Result;

        async void WebViewGoBack()
        {
            await DoOnMainThreadAsync(() => webView.GoBack()).ConfigureAwait(false);
        }
        async void WebViewGoForward()
        {
            await DoOnMainThreadAsync(() => webView.GoForward()).ConfigureAwait(false);
        }

        public double DeviceWidth
        {
            get
            {
                return DoOnMainThreadAsync(() => webView.ActualWidth).Result;
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


#if WINDOWS_PHONE_APP
        public const bool HaveClipboard = false;
#else
        public const bool HaveClipboard = true;
#endif

        public void CopyToClipboard(string str, string type)
        {
#if !WINDOWS_PHONE_APP
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
#endif
        }

        MenuFlyout contextMenu;
        public void HideContextMenu()
        {
            contextMenu.Hide();
        }

        public async void ShowContextMenu(double x, double y, string url, string innerText)
        {
            HideContextMenu();

#if !WINDOWS_UWP
            Xamarin.Forms.AbsoluteLayout.SetLayoutBounds(contextMenuPlaceholder, new Xamarin.Forms.Rectangle(x / webView.ActualWidth, y / webView.ActualHeight, 0, 0));
#endif
            var param = await GetContextMenuParamAsync(url, innerText);

            bool had = false;
            foreach (var baseitem in contextMenu.Items)
            {
                if (baseitem is MenuFlyoutItem item)
                {
                    item.CommandParameter = param;
                    if (item.Command != null)
                    {
                        item.Visibility = item.Command.CanExecute(param) ? Visibility.Visible : Visibility.Collapsed;
                        if (item.Visibility == Visibility.Visible) had = true;
                    }
                }
                else if (baseitem is WinRT.MenuFlyoutSeparatorEx separator && separator.Command != null)
                {
                    separator.Visibility = separator.Command.CanExecute(param) ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            if (!had) return;

#if WINDOWS_UWP
            contextMenu.ShowAt(webView, new Windows.Foundation.Point(x, y));
#else
            var renderer = Xamarin.Forms.Platform.WinRT.Platform.GetRenderer(contextMenuPlaceholder);
            contextMenu.ShowAt(renderer.ContainerElement);
#endif
        }
    }
}
