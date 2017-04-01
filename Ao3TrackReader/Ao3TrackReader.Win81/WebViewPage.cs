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
using Xamarin.Forms.Platform.WinRT;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using System.Threading;
using Ao3TrackReader.Data;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.ApplicationModel.DataTransfer;
using Newtonsoft.Json;

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

        private CoreDispatcher Dispatcher { get; } = CoreWindow.GetForCurrentThread().Dispatcher;

        public bool IsMainThread => Dispatcher.HasThreadAccess; 

        WebView webView;
        Xamarin.Forms.View contextMenuPlaceholder;


        public Xamarin.Forms.View CreateWebView()
        {
            webView = new WebView()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            webView.NavigationStarting += WebView_NavigationStarting;
            webView.DOMContentLoaded += WebView_DOMContentLoaded;
            webView.ContentLoading += WebView_ContentLoading;
            webView.GotFocus += WebView_GotFocus;
            webView.DefaultBackgroundColor = Ao3TrackReader.Resources.Colors.Alt.MediumHigh.ToWindows();
            var helper = new Ao3TrackHelper(this);
            var messageHandler = new ScriptMessageHandler(this, helper);
            webView.ScriptNotify += messageHandler.WebView_ScriptNotify;
            this.helper = helper;

            contextMenuPlaceholder = new Xamarin.Forms.ContentView();
            Xamarin.Forms.AbsoluteLayout.SetLayoutBounds(contextMenuPlaceholder, new Xamarin.Forms.Rectangle(0, 0, 0, 0));
            Xamarin.Forms.AbsoluteLayout.SetLayoutFlags(contextMenuPlaceholder, Xamarin.Forms.AbsoluteLayoutFlags.None);

            webView.SizeChanged += WebView_SizeChanged;

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

        private void WebView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CallJavascriptAsync("Ao3Track.Win81.helper.setValue", "deviceWidth", DeviceWidth).Wait(0);
        }

        private void WebView_GotFocus(object sender, RoutedEventArgs e)
        {
            OnWebViewGotFocus();
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
        }

        async Task OnInjectingScripts(CancellationToken ct)
        {
            await EvaluateJavascriptAsync("window.Ao3TrackHelperNative = " + helper.HelperDefJson + ";");
        }

        Task OnInjectedScripts(CancellationToken ct)
        {
            return Task.FromResult(new object());
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
            var newuri = Ao3SiteDataLookup.CheckUri(uri);
            if (newuri == null)
            {
                OpenExternal(uri);
                return;
            }

            helper?.Reset();
            webView.Navigate(newuri);
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
#if WINDOWS_APP
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

        public async void ShowContextMenu(double x, double y, string url, string innerHtml)
        {
            HideContextMenu();

            Xamarin.Forms.AbsoluteLayout.SetLayoutBounds(contextMenuPlaceholder, new Xamarin.Forms.Rectangle(x * Width / webView.Width, y * Height / webView.Height, 0, 0));

            var inturl = Ao3SiteDataLookup.CheckUri(new Uri(url)) != null;
            var res = inturl ? await AreUrlsInReadingListAsync(new[] { url }) : null;
            ContextMenuOpenAdd.IsEnabled = inturl && !res[url];
            ContextMenuAdd.IsEnabled = inturl && !res[url];
            ContextMenuRemove.IsEnabled = inturl && res[url];

            foreach (var baseitem in contextMenu.Items)
            {
                if (baseitem is MenuFlyoutItem item)
                {
                    item.CommandParameter = url;
                    if (item.Command != null)
                        item.Visibility = item.Command.CanExecute(url) ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            var renderer = Xamarin.Forms.Platform.WinRT.Platform.GetRenderer(contextMenuPlaceholder);
            contextMenu.ShowAt(renderer.ContainerElement);
        }

        class ScriptMessageHandler
        {
            WebViewPage wvp;
            private Ao3TrackHelper helper;

            public ScriptMessageHandler(WebViewPage wvp, Ao3TrackHelper helper)
            {
                this.wvp = wvp;
                this.helper = helper;
            }

            public class Message
            {
                public string type { get; set; }
                public string name { get; set; }
                public string value { get; set; }
                public string[] args { get; set; }
            }

            private object Deserialize(string value, Type type)
            {
                // If destination is a string, then the value passes through unchanged. A minor optimization
                if (type == typeof(string)) return value;
                else return JsonConvert.DeserializeObject(value, type);
            }

            public void WebView_ScriptNotify(object sender, NotifyEventArgs ea)
            {
                var smsg = ea.Value;
                var msg = JsonConvert.DeserializeObject<Message>(smsg);

                if (msg.type == "INIT")
                {
                    wvp.DoOnMainThread(async () =>
                    {
                        await wvp.EvaluateJavascriptAsync(string.Format(
                            "Ao3Track.Win81.helper.setValue({0},{1});Ao3Track.Win81.helper.setValue({2},{3});Ao3Track.Win81.helper.setValue({4},{5});Ao3Track.Win81.helper.setValue({6},{7});",
                            JsonConvert.SerializeObject("leftOffset"), JsonConvert.SerializeObject(wvp.LeftOffset),
                            JsonConvert.SerializeObject("swipeCanGoBack"), JsonConvert.SerializeObject(wvp.SwipeCanGoBack),
                            JsonConvert.SerializeObject("swipeCanGoForward"), JsonConvert.SerializeObject(wvp.SwipeCanGoForward),
                            JsonConvert.SerializeObject("deviceWidth"), JsonConvert.SerializeObject(wvp.DeviceWidth)));
                    });
                    return;
                }
                else if (helper.HelperDef.TryGetValue(msg.name, out var md))
                {
                    if (msg.type == "SET" && md.pi?.CanWrite == true)
                    {
                        md.pi.SetValue(wvp.helper, Deserialize(msg.value, md.pi.PropertyType));
                        return;
                    }
                    else if (msg.type == "CALL" && md.mi != null)
                    {
                        var ps = md.mi.GetParameters();
                        if (msg.args.Length == ps.Length)
                        {
                            var args = new object[msg.args.Length];
                            for (int i = 0; i < msg.args.Length; i++)
                            {
                                args[i] = Deserialize(msg.args[i], ps[i].ParameterType);
                            }

                            md.mi.Invoke(wvp.helper, args);
                            return;
                        }
                    }
                }

                throw new ArgumentException();
            }

        }

    }
}
