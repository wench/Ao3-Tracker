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

namespace Ao3TrackReader
{
    public partial class WebViewPage : IWebViewPage
    {
        public string JavaScriptInject { get; } = @"(function(){
            var head = document.getElementsByTagName('head')[0];
            var toInject = JSON.parse(Ao3TrackHelperWebkit.get_CssToInject());
            console.log(toInject);
            for (var i = 0; i< toInject.length; i++) {                    
                var link = document.createElement('link');
                link.type = 'text/css';
                link.rel = 'stylesheet';
                link.href = toInject[i];
                head.appendChild(link);
            }
            toInject = JSON.parse(Ao3TrackHelperWebkit.get_ScriptsToInject());
            console.log(toInject);
            for (var i = 0; i< toInject.length; i++) {                    
                var script = document.createElement('script');
                script.type = 'text/javascript';
                script.src = toInject[i];
                head.appendChild(script);
            }
        })();";

        public string[] ScriptsToInject { get; } =
            new[] {
                "https://ao3track.wenchy.net/polyfills.js",
                "https://ao3track.wenchy.net/marshal.js",
                "https://ao3track.wenchy.net/callbacks.js",
                "https://ao3track.wenchy.net/platform.js",
                "https://ao3track.wenchy.net/reader.js",
                "https://ao3track.wenchy.net/tracker.js",
                "https://ao3track.wenchy.net/unitconv.js",
                "https://ao3track.wenchy.net/touch.js"
            };

        public string[] CssToInject { get; } = { "https://ao3track.wenchy.net/tracker.css" };


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
            WebView.Settings.AllowFileAccess = true;
            WebView.Settings.AllowFileAccessFromFileURLs = true;
            WebView.Settings.AllowContentAccess = true;
            WebView.Settings.JavaScriptEnabled = true;
            WebView.Settings.AllowUniversalAccessFromFileURLs = true;
            WebView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;

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
            WebView.AddJavascriptInterface((Java.Lang.Object) obj, name);
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

        public bool CanGoBack { get { return WebView.CanGoBack() || prevPage != null; } }

        public bool CanGoForward { get { return WebView.CanGoForward() || nextPage != null; } }

        public void GoBack()
        {
            if (WebView.CanGoBack()) WebView.GoBack();
            else if (prevPage != null) WebView.LoadUrl(prevPage.AbsoluteUri);
        }
        public void GoForward()
        {
            if (WebView.CanGoForward()) WebView.GoForward();
            else if (nextPage != null) WebView.LoadUrl(nextPage.AbsoluteUri);
        }
        
        public double LeftOffset
        {
            get
            {
                return WebView.TranslationX;
            }

            set
            {
                WebView.TranslationX = (float)value;
            }
        }
        
        TaskCompletionSource<string> contextMenuResult = null;
        Android.Widget.PopupMenu contextMenu = null;
        public void CloseContextMenu()
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
            CloseContextMenu();

            contextMenuResult = new TaskCompletionSource<string>();

            Xamarin.Forms.AbsoluteLayout.SetLayoutBounds(contextMenuPlaceholder, new Rectangle(x, y, 0, 0));
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
            public override void OnLoadResource(WebView view, string url)
            {
                base.OnLoadResource(view, url);
            }

            public override WebResourceResponse ShouldInterceptRequest(WebView view, IWebResourceRequest request)
            {
                if (request.Url.Host == "ao3track.wenchy.net")
                {
                    try
                    {
                        string file = request.Url.LastPathSegment;
                        string mime;
                        string encoding = "UTF-8";
                        switch (System.IO.Path.GetExtension(file))
                        {
                            case ".js":
                                mime = "application/javascript";
                                break;

                            case ".css":
                                mime = "text/css";
                                break;

                            case ".jpg":
                            case ".jpeg":
                                mime = "image/jpeg";
                                encoding = "cp1252";
                                break;

                            case ".png":
                                mime = "image/png";
                                encoding = "cp1252";
                                break;

                            case ".gif":
                                mime = "image/gif";
                                encoding = "cp1252";
                                break;

                            case ".htm":
                            case ".html":
                                mime = "text/html";
                                break;

                            default:
                                mime = "text/plain";
                                break;
                        }


                        return new WebResourceResponse(
                            mime,
                            encoding,
                            Forms.Context.Assets.Open(file)
                            );
                    }
                    catch
                    {
                    }
                }

                return base.ShouldInterceptRequest(view, request);
            }

            public override void OnPageFinished(WebView view, string url)
            {
                base.OnPageFinished(view, url);
                wvp.OnContentLoaded();
            }

            public override void OnPageStarted(WebView view, string url, Bitmap favicon)
            {
                base.OnPageStarted(view, url, favicon);
                wvp.AddJavascriptObject("Ao3TrackHelperNative", wvp.helper);
                wvp.OnContentLoading();
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