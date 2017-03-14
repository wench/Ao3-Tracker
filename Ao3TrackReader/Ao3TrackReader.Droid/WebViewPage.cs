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

using System.Threading.Tasks;
//using Xamarin.Forms;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using System.Runtime.InteropServices.WindowsRuntime;
using Android.Webkit;
using WebView = Android.Webkit.WebView;
using Android.Graphics;

using Ao3TrackReader.Helper;
using Ao3TrackReader.Droid;
using Ao3TrackReader.Data;
using Java.Lang;
using System.IO;
using System.Threading;

namespace Ao3TrackReader
{
    public partial class WebViewPage : IWebViewPage
    {
        public string[] ScriptsToInject { get; } =
            new[] {
                "jquery-3.1.1.js",
                "polyfills.js",
                "marshal.js",
                "callbacks.js",
                "platform.js",
                "reader.js",
                "tracker.js",
                "unitconv.js",
                "touch.js"
            };

        public string[] CssToInject { get; } = { "tracker.css" };

        public bool IsMainThread
        {
            get { return Looper.MainLooper == Looper.MyLooper(); }
        }


        WebView webView;
        WebClient webClient;
        Xamarin.Forms.View contextMenuPlaceholder;

        public bool ShowBackOnToolbar { get {
                return true;
            } }

        public Xamarin.Forms.View CreateWebView()
        {
            webView = new WebView(Forms.Context);
            webView.SetWebViewClient(webClient = new WebClient(this));
            webView.SetWebChromeClient(new ChromeClient(this));
            webView.Settings.AllowContentAccess = true;
            webView.Settings.JavaScriptEnabled = true;
            webView.Settings.BuiltInZoomControls = true;
            webView.Settings.DisplayZoomControls = false;
            webView.Settings.UseWideViewPort = true;
            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
#if DEBUG
                WebView.SetWebContentsDebuggingEnabled(true);
#else
                WebView.SetWebContentsDebuggingEnabled(false);
#endif
            }
            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                webView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
            }
            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                webView.Settings.DisabledActionModeMenuItems = MenuItems.ProcessText | MenuItems.Share | MenuItems.WebSearch;
            }

            AddJavascriptObject("Ao3TrackHelperNative", helper);

            var placeholder = new Android.Views.View(Forms.Context);
            contextMenuPlaceholder = placeholder.ToView();
            Xamarin.Forms.AbsoluteLayout.SetLayoutBounds(contextMenuPlaceholder, new Rectangle(0, 0, 0, 0));
            Xamarin.Forms.AbsoluteLayout.SetLayoutFlags(contextMenuPlaceholder, AbsoluteLayoutFlags.None);
            helper = new Ao3TrackHelper(this);

            contextMenu = new PopupMenu(Forms.Context, placeholder);
            var menu = contextMenu.Menu;

            for (int i = 0; i < ContextMenuItems.Count; i++)
            {
                var kvp = ContextMenuItems[i];
                if (kvp.Key == "-")
                {
                    menu.Add(Menu.None, i, i, "-").SetEnabled(false);
                }
                else
                {
                    menu.Add(Menu.None, i, i, kvp.Key);
                }
            }

            contextMenu.MenuItemClick += (s1, arg1) =>
            {
                if (ContextMenuItems[arg1.Item.ItemId].Value != null)
                    ContextMenuItems[arg1.Item.ItemId].Value.Execute(contextMenuUrl);
            };

            MainContent.Children.Add(contextMenuPlaceholder);

            webView.FocusChange += WebView_FocusChange;

            return webView.ToView();
        }

        private void WebView_FocusChange(object sender, Android.Views.View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
            {
                OnWebViewGotFocus();
            }
        }

        public class ValueCallback : Java.Lang.Object, IValueCallback
        {
            Action<string> callback;
            public ValueCallback(Action<string> callback)
            {
                this.callback = callback;
            }

            public void OnReceiveValue(Java.Lang.Object value)
            {
                if (value != null) callback(value.ToString());
                else callback(null);
            }
        }

        public async Task<string> EvaluateJavascriptAsync(string code)
        {
            var cs = new TaskCompletionSource<string>();
            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                DoOnMainThread(() => webView.EvaluateJavascript(code, new ValueCallback((value) => { cs.SetResult(value); })));
            }
            else
            {
                webView.LoadUrl(helper.GetEvalJavascriptUrl(code,cs));
            }
            return await cs.Task;
        }

        public void AddJavascriptObject(string name, object obj)
        {
            webView.AddJavascriptInterface((Java.Lang.Object)obj, name);
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
            using (StreamReader sr = new StreamReader(Forms.Context.Assets.Open(name), Encoding.UTF8))
            {
                return await sr.ReadToEndAsync();
            }
        }

        public Uri CurrentUri
        {
            get
            {
                return DoOnMainThread(() =>
                {
                    if (!string.IsNullOrWhiteSpace(webView.Url)) return new Uri(webView.Url);
                    else return new Uri("about:blank");
                });
            }
        }

        public void Navigate(Uri uri)
        {
            helper?.Reset();
            OnNavigationStarting(uri);
            webView.LoadUrl(uri.AbsoluteUri);
        }

        public void Refresh()
        {
            webView.Reload();
        }

        public bool SwipeCanGoBack { get { return webView.CanGoBack() || prevPage != null; } }

        public bool SwipeCanGoForward { get { return webView.CanGoForward() || nextPage != null; } }

        public void SwipeGoBack()
        {
            if (webView.CanGoBack()) webView.GoBack();
            else if (prevPage != null) Navigate(prevPage);
        }
        public void SwipeGoForward()
        {
            if (webView.CanGoForward()) webView.GoForward();
            else if (nextPage != null) Navigate(nextPage);
        }

        public double DeviceWidth
        {
            get
            {
                return webView.MeasuredWidth;
            }
        }

        public double LeftOffset
        {
            get
            {
                return webView.TranslationX;
            }

            set
            {
                webView.TranslationX = (float)(value);
            }
        }

        public void CopyToClipboard(string str, string type)
        {
            var clipboard = Xamarin.Forms.Forms.Context.GetSystemService(Context.ClipboardService) as ClipboardManager;
            if (type == "text")
            {
                ClipData clip = ClipData.NewPlainText("Text from Ao3", str);
                clipboard.PrimaryClip = clip;
            }
            else if (type == "uri")
            {
                ClipData clip = ClipData.NewRawUri(str, Android.Net.Uri.Parse(str));
                clipboard.PrimaryClip = clip;
            }
        }

        Android.Widget.PopupMenu contextMenu;
        string contextMenuUrl;
        public void HideContextMenu()
        {
            contextMenu.Dismiss();
        }

        public async void ShowContextMenu(double x, double y, string url, string innerHtml)
        {
            HideContextMenu();

            Xamarin.Forms.AbsoluteLayout.SetLayoutBounds(contextMenuPlaceholder, new Rectangle(x* Width / webView.Width, y * Height / webView.Height, 0, 0));

            var res = await AreUrlsInReadingListAsync(new[] { url });
            ContextMenuOpenAdd.IsEnabled = !res[url];
            ContextMenuAdd.IsEnabled = !res[url];

            for (int i = 0; i < ContextMenuItems.Count; i++)
            {
                if (ContextMenuItems[i].Value != null)
                    contextMenu.Menu.GetItem(i).SetEnabled(ContextMenuItems[i].Value.CanExecute(url));
            }

            contextMenuUrl = url;

            contextMenu.Show();
        }

        class WebClient : WebViewClient
        {
            WebViewPage wvp;

            public WebClient(WebViewPage wvp)
            {
                this.wvp = wvp;
            }

            bool canDoOnContentLoaded = false;
            public override void OnPageFinished(WebView view, string url)
            {
                var uri = new Uri(url);

                base.OnPageFinished(view, url);
                if (canDoOnContentLoaded)
                {
                    wvp.OnContentLoaded();
                    canDoOnContentLoaded = false;
                }
            }

            public override void OnPageStarted(WebView view, string url, Bitmap favicon)
            {
                base.OnPageStarted(view, url, favicon);
                wvp.OnContentLoading();
                canDoOnContentLoaded = true;
                wvp.AddJavascriptObject("Ao3TrackHelperNative", wvp.helper);
            }

            public override void OnPageCommitVisible(WebView view, string url)
            {
                base.OnPageCommitVisible(view, url);
            }

            public override void DoUpdateVisitedHistory(WebView view, string url, bool isReload)
            {
                base.DoUpdateVisitedHistory(view, url, isReload);
            }

            [Obsolete]
            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                return wvp.OnNavigationStarting(new Uri(url));
            }

            public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
            {
                return wvp.OnNavigationStarting(new Uri(request.Url.ToString()));
            }

            public override async void OnScaleChanged(WebView view, float oldScale, float newScale)
            {
                base.OnScaleChanged(view, oldScale, newScale);
                await wvp.CallJavascriptAsync("Ao3Track.Touch.updateTouchState",Array.Empty<object>());
            }
        }

        class ChromeClient : WebChromeClient
        {
            WebViewPage wvp;

            public ChromeClient(WebViewPage wvp)
            {
                this.wvp = wvp;
            }

            public override bool OnConsoleMessage(ConsoleMessage consoleMessage)
            {
                int lineNumber = consoleMessage.LineNumber();
                string message = consoleMessage.Message();
                var messageLevel = consoleMessage.InvokeMessageLevel();
                var sourceId = consoleMessage.SourceId() ?? "";
                if (sourceId.StartsWith("https://ao3track.wenchy.net/")) sourceId = "Assets/"+sourceId.Substring(28);
                System.Diagnostics.Debug.WriteLine(string.Format(" {0}({1}): {2}: {3}",sourceId,lineNumber,messageLevel.Name(),message));
                return true;
            }
            [Obsolete]
            public override void OnConsoleMessage(string message, int lineNumber, string sourceID)
            {
                return;
            }
        }

    }

}