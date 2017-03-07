﻿/*
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

        void OnInjectingScripts()
        {
        }

        void OnInjectedScripts()
        {
        }

        async Task<string> ReadFile(string name)
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Content/" + name));
            return await Windows.Storage.FileIO.ReadTextAsync(file);
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

        public bool SwipeCanGoBack { get { return WebView.CanGoBack || prevPage != null; } }

        public bool SwipeCanGoForward { get { return WebView.CanGoForward || nextPage != null; } }

        public void SwipeGoBack()
        {
            if (WebView.CanGoBack) WebView.GoBack();
            else if (prevPage != null) Navigate(prevPage);
        }
        public void SwipeGoForward()
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
