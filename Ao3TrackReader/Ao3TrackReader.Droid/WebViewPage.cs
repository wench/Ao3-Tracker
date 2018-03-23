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
using Android.Net.Http;

namespace Ao3TrackReader
{
    public partial class WebViewPage : IWebViewPage
    {
        public bool IsMainThread
        {
            get { return Looper.MainLooper == Looper.MyLooper(); }
        }

        internal IVisualElementRenderer MainLayoutRenderer => MainLayout.GetRenderer();

        class MyWebView : Android.Webkit.WebView, IMenuItemOnMenuItemClickListener
        {
            WebViewPage wvp;

            public MyWebView(Context context, WebViewPage wvp) : base(context) {
                this.wvp = wvp;
            }

            public override ActionMode StartActionMode(ActionMode.ICallback callback)
            {

                var actionMode = base.StartActionMode(callback);
                var menu = actionMode.Menu;
                /*int lastid = 0;
                for (int i = 0; i < menu.Size(); i++)
                {
                    var item = menu.GetItem(i);
                    var title = item.TitleFormatted.ToString();
                    lastid = item.ItemId;
                }
                var name = Resources.GetResourceName(lastid);*/

                var id = Resources.GetIdentifier("select_action_menu_web_search", "id", "com.android.webview");
                if (id == 0) id = Resources.GetIdentifier("webviewchromium_select_action_menu_web_search", "id", "android");
                if (id == 0) id = Resources.GetIdentifier("websearch", "id", "android");
                if (id != 0)
                {
                    var search = menu.FindItem(id);
                    search?.SetOnMenuItemClickListener(this);
                }

                return actionMode;
            }

            bool IMenuItemOnMenuItemClickListener.OnMenuItemClick(IMenuItem item)
            {
                wvp.DoOnMainThreadAsync(async() => {
                    string value = await wvp.EvaluateJavascriptAsync("window.getSelection().toString()");
                    ClearFocus();
                    try
                    {
                        value = Newtonsoft.Json.JsonConvert.DeserializeObject(value).ToString();
                        wvp.GoogleSearch(value);
                    }
                    catch
                    {
                    }
                });
                return true;
            }
        }


        MyWebView webView;
        WebClient webClient;
        Xamarin.Forms.View contextMenuPlaceholder;

        public bool ShowBackOnToolbar { get {
                return true;
            } }

        public Xamarin.Forms.View CreateWebView()
        {
            webView = new MyWebView(MainActivity.Instance, this);
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

            contextMenu = new ContextMenu(Forms.Context, placeholder);
            var menu = contextMenu.Menu;
            for (int i = 0; i < ContextMenuItems.Count; i++)
            {
                var kvp = ContextMenuItems[i];
                if (kvp.Key == "-")
                {
                    // Looks better without
                    //menu.Add(Menu.None, i, i, "\x23AF\x23AF\x23AF\x23AF").SetEnabled(false);
                }
                else
                {
                    menu.Add(Android.Views.Menu.None, i, i, kvp.Key);
                }
            }

            contextMenu.MenuItemClick += (sender, arg) =>
            {
                ContextMenuItems[arg.Item.ItemId].Value?.Execute(contextMenu.CommandParameter);
            };

            MainContent.Children.Add(contextMenuPlaceholder);

            webView.FocusChange += WebView_FocusChange;

            return webView.ToView();
        }

        public void ShowErrorPage(string message, Uri uri)
        {
            var html = GetErrorPageHtml(message, uri);
            webView.LoadDataWithBaseURL(null, html, "text/html", "UTF-8", null);
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
                await DoOnMainThreadAsync(() => webView.EvaluateJavascript(code, new ValueCallback((value) => { cs.SetResult(value); })));
            }
            else
            {
                webView.LoadUrl(helper.GetEvalJavascriptUrl(code, cs));
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
                return DoOnMainThreadAsync(() =>
                {
                    if (!string.IsNullOrWhiteSpace(webView.Url)) return new Uri(webView.Url);
                    else return new Uri("about:blank");
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
            if (OnNavigationStarting(newuri) == false)
                webView.LoadUrl(newuri.AbsoluteUri);
        }

        public void Refresh()
        {
            if (!string.IsNullOrWhiteSpace(webView.Url) && OnNavigationStarting(new Uri(webView.Url)) == false)
            {
                webView.Reload();
            }
        }

        bool WebViewCanGoBack => webView.CanGoBack();

        bool WebViewCanGoForward => webView.CanGoForward();

        void WebViewGoBack()
        {
            if (webView.CanGoBack())
            {
                var history = webView.CopyBackForwardList();
                var url = history.GetItemAtIndex(history.CurrentIndex - 1);                
                if (OnNavigationStarting(new Uri(url.Url)) == false)
                    webView.GoBack();
            }
            else
            {
                LeftOffset = 0.0;
            }
        }
        void WebViewGoForward()
        {
            if (webView.CanGoForward())
            {
                var history = webView.CopyBackForwardList();
                var url = history.GetItemAtIndex(history.CurrentIndex + 1);
                if (OnNavigationStarting(new Uri(url.Url)) == false)
                    webView.GoForward();
            }
            else
            {
                LeftOffset = 0.0;
            }
        }

        public double DeviceWidth
        {
            get
            {
                double w = webView.MeasuredWidth;
                if (w == 0) w = webView.Width;
                return w;
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

        public const bool HaveClipboard = true;

        public void CopyToClipboard(string str, string type)
        {
            var clipboard = webView.Context.GetSystemService(Context.ClipboardService) as ClipboardManager;
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

        class ContextMenu : Android.Widget.PopupMenu
        {
            public ContextMenu(Context context, Android.Views.View anchor) : base(context, anchor) { }

            public ContextMenuParam CommandParameter { get; set; }
        }


        ContextMenu contextMenu;

        public void HideContextMenu()
        {
            contextMenu.Dismiss();
        }

        public async void ShowContextMenu(double x, double y, string url, string innerText)
        {
            HideContextMenu();

            Xamarin.Forms.AbsoluteLayout.SetLayoutBounds(contextMenuPlaceholder, new Rectangle(x* Width / webView.Width, y * Height / webView.Height, 0, 0));
            contextMenu.CommandParameter = await GetContextMenuParamAsync(url, innerText);

            bool had = false;

            for (int i = 0; i < ContextMenuItems.Count; i++)
            {
                if (ContextMenuItems[i].Value != null)
                {
                    bool vis = ContextMenuItems[i].Value.CanExecute(contextMenu.CommandParameter);
                    contextMenu.Menu.FindItem(i)?.SetVisible(vis);
                    if (vis) had = true;
                }
            }

            if (!had) return;

            contextMenu.Show();
        }

        bool doLoading = false;
        bool doLoaded = false;

        class WebClient : WebViewClient
        {
            WebViewPage wvp;

            public WebClient(WebViewPage wvp)
            {
                this.wvp = wvp;
            }

            string allowedUrl;
            public override void OnPageFinished(WebView view, string url)
            {
                var uri = new Uri(url);

                base.OnPageFinished(view, url);
                System.Diagnostics.Debug.WriteLine($"OnPageFinished: {url}");
            }

            public override void OnPageStarted(WebView view, string url, Bitmap favicon)
            {
                base.OnPageStarted(view, url, favicon);

                System.Diagnostics.Debug.WriteLine($"OnPageStarted: {url}");
                wvp.AddJavascriptObject("Ao3TrackHelperNative", wvp.helper);
                wvp.doLoaded = true;
                wvp.doLoading = true;
            }

            public override void OnPageCommitVisible(WebView view, string url)
            {
                System.Diagnostics.Debug.WriteLine($"OnPageCommitVisible: {url}");
                base.OnPageCommitVisible(view, url);
            }

            public override void DoUpdateVisitedHistory(WebView view, string url, bool isReload)
            {
                base.DoUpdateVisitedHistory(view, url, isReload);
            }

            [Obsolete]
            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                if (wvp.OnNavigationStarting(new Uri(url)))
                    return true;
                allowedUrl = url;
                System.Diagnostics.Debug.WriteLine($"ShouldOverrideUrlLoading: {allowedUrl}");
                return base.ShouldOverrideUrlLoading(view, url);
            }

            public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
            {
                if (wvp.OnNavigationStarting(new Uri(request.Url.ToString())))
                    return true;
                allowedUrl = request.Url.ToString();
                System.Diagnostics.Debug.WriteLine($"ShouldOverrideUrlLoading: {allowedUrl}");

                return false;
            }

            public override async void OnScaleChanged(WebView view, float oldScale, float newScale)
            {
                base.OnScaleChanged(view, oldScale, newScale);
                await wvp.EvaluateJavascriptAsync("try { Ao3Track.Touch.updateTouchState(); } catch(exp) { }");
            }

            [Obsolete]
            public override void OnReceivedError(WebView view, ClientError errorCode, string description, string failingUrl)
            {
                wvp.ShowErrorPage(description, new Uri(failingUrl));
            }

            public override void OnReceivedError(WebView view, IWebResourceRequest request, WebResourceError error)
            {
                if (request.IsForMainFrame)
                {
                    wvp.ShowErrorPage(error.Description.ToString(), new Uri(request.Url.ToString()));
                }
            }

            public override void OnReceivedHttpError(WebView view, IWebResourceRequest request, WebResourceResponse errorResponse)
            {
                if (request.IsForMainFrame)
                {
                    wvp.ShowErrorPage(errorResponse.ReasonPhrase, new Uri(request.Url.ToString()));
                }
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

            public override void OnReceivedTitle(WebView view, string title)
            {
                System.Diagnostics.Debug.WriteLine($"Title: {title}");

                wvp.DoOnMainThreadAsync(() =>
                {
                    if (wvp.doLoading)
                    {
                        wvp.OnContentLoading();
                        wvp.doLoading = false;
                    }
                });

                base.OnReceivedTitle(view, title);
            }

            public override void OnProgressChanged(WebView view, int newProgress)
            {
                base.OnProgressChanged(view, newProgress);
                if (view != wvp.webView) return;
                System.Diagnostics.Debug.WriteLine($"Load Progress: {newProgress}");

                wvp.DoOnMainThreadAsync(() =>
                {
                    if (wvp.doLoading && newProgress >= 50)
                    {
                        wvp.doLoading = false;
                        wvp.DoOnMainThreadAsync(() => wvp.OnContentLoading());
                    }
                    if (wvp.doLoaded && newProgress >= 100)
                    {
                        wvp.doLoaded = false;
                        wvp.OnContentLoaded();
                    }
                });
            }
        }

    }

}