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


        WebView WebView { get; set; }
        WebClient webClient;
        Xamarin.Forms.View contextMenuPlaceholder;

        public bool ShowBackOnToolbar { get {
                return true;
            } }

        public Xamarin.Forms.View CreateWebView()
        {
            WebView = new WebView(Forms.Context);
            WebView.SetWebViewClient(webClient = new WebClient(this));
            WebView.SetWebChromeClient(new ChromeClient(this));
            WebView.Settings.AllowContentAccess = true;
            WebView.Settings.JavaScriptEnabled = true;
            WebView.Settings.BuiltInZoomControls = true;
            WebView.Settings.DisplayZoomControls = false;
            WebView.Settings.UseWideViewPort = true;
            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                WebView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
            }
            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                WebView.Settings.DisabledActionModeMenuItems = MenuItems.ProcessText | MenuItems.Share | MenuItems.WebSearch;
            }
#if DEBUG
            WebView.SetWebContentsDebuggingEnabled(true);
#else
            WebView.SetWebContentsDebuggingEnabled(false);

#endif
            AddJavascriptObject("Ao3TrackHelperNative", helper);

            contextMenuPlaceholder = (new Android.Views.View(Forms.Context)).ToView();
            Xamarin.Forms.AbsoluteLayout.SetLayoutBounds(contextMenuPlaceholder, new Rectangle(0, 0, 0, 0));
            Xamarin.Forms.AbsoluteLayout.SetLayoutFlags(contextMenuPlaceholder, AbsoluteLayoutFlags.None);
            helper = new Ao3TrackHelper(this);

            WebView.FocusChange += WebView_FocusChange;

            return WebView.ToView();
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
            DoOnMainThread(() => WebView.EvaluateJavascript(code, new ValueCallback((value) => { cs.SetResult(value); })));

            return await cs.Task;
        }

        public void AddJavascriptObject(string name, object obj)
        {
            WebView.AddJavascriptInterface((Java.Lang.Object)obj, name);
        }

        void OnInjectingScripts()
        {
        }

        void OnInjectedScripts()
        {
        }

        async Task<string> ReadFile(string name)
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
                return DoOnMainThread(() => new Uri(WebView.Url));
            }
        }

        public void Navigate(Uri uri)
        {
            helper?.Reset();
            WebView.LoadUrl(uri.AbsoluteUri);
        }

        public void Refresh()
        {
            WebView.Reload();
        }

        public bool SwipeCanGoBack { get { return WebView.CanGoBack() || prevPage != null; } }

        public bool SwipeCanGoForward { get { return WebView.CanGoForward() || nextPage != null; } }

        public void SwipeGoBack()
        {
            if (WebView.CanGoBack()) WebView.GoBack();
            else if (prevPage != null) WebView.LoadUrl(prevPage.AbsoluteUri);
        }
        public void SwipeGoForward()
        {
            if (WebView.CanGoForward()) WebView.GoForward();
            else if (nextPage != null) WebView.LoadUrl(nextPage.AbsoluteUri);
        }

        public double DeviceWidth
        {
            get
            {
                return WebView.MeasuredWidth;
            }
        }

        public double LeftOffset
        {
            get
            {
                return WebView.TranslationX;
            }

            set
            {
                WebView.TranslationX = (float)(value);
            }
        }
        
        TaskCompletionSource<string> contextMenuResult = null;
        Android.Widget.PopupMenu contextMenu = null;
        public void HideContextMenu()
        {
            if (contextMenuResult != null)
            {
                contextMenuResult.TrySetCanceled();
                contextMenuResult = null;
            }
            if (contextMenu != null)
            {
                contextMenu.Dismiss();
                contextMenu = null;
            }
        }

        public Task<string> ShowContextMenu(double x, double y, string[] menuItems)
        {
            HideContextMenu();

            contextMenuResult = new TaskCompletionSource<string>();

            Xamarin.Forms.AbsoluteLayout.SetLayoutBounds(contextMenuPlaceholder, new Rectangle(x* Width / WebView.Width, y * Height / WebView.Height, 0, 0));
            MainContent.Children.Add(contextMenuPlaceholder);
            var renderer = Platform.GetRenderer(contextMenuPlaceholder) as NativeViewWrapperRenderer;

            contextMenu = new PopupMenu(Forms.Context, renderer.Control);
            var menu = contextMenu.Menu;

            for (int i = 0; i < menuItems.Length; i++)
            {
                if (menuItems[i] == "-")
                {
                    menu.Add(Menu.None, i, i, "-").SetEnabled(false);
                }
                else
                {
                    menu.Add(Menu.None, i, i, menuItems[i]);
                }
            }

            contextMenu.MenuItemClick += (s1, arg1) =>
            {
                contextMenuResult.TrySetResult(menuItems[arg1.Item.ItemId]);
            };

            contextMenu.DismissEvent += (s2, arg2) =>
            {
                contextMenuResult.TrySetResult("");
                MainContent.Children.Remove(contextMenuPlaceholder);
            };

            contextMenu.Show();

            return contextMenuResult.Task.ContinueWith((task) => {
                contextMenu = null;
                contextMenuResult = null;
                return task.Result;
            });
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
                var sourceId = consoleMessage.SourceId();
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