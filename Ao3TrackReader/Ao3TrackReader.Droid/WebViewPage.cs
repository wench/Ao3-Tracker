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
    public partial class WebViewPage : IEventHandler
    {
        const string JavaScriptInject = @"(function(){
            var head = document.getElementsByTagName('head')[0];
            var toInject = JSON.parse(Ao3TrackHelper.get_CssToInject());
            for (var i = 0; i< toInject.length; i++) {                    
                var link = document.createElement('link');
                link.type = 'text/css';
                link.rel = 'stylesheet';
                link.href = toInject[i];
                head.appendChild(link);
            }
            toInject = JSON.parse(Ao3TrackHelper.get_ScriptsToInject());
            for (var i = 0; i< toInject.length; i++) {                    
                var script = document.createElement('script');
                script.type = 'text/javascript';
                script.src = toInject[i];
                head.appendChild(script);
            }
        })();";

        public string[] scriptsToInject
        {
            get { return new[] { "https://ao3track.wenchy.net/ao3_t_callbacks.js", "https://ao3track.wenchy.net/ao3_t_reader.js", "https://ao3track.wenchy.net/ao3_tracker.js", "https://ao3track.wenchy.net/ao3_t_unitconv.js" }; }
        }
        public string[] cssToInject
        {
            get { return new[] { "https://ao3track.wenchy.net/ao3_tracker.css" }; }

        }

        Ao3TrackHelper helper;

        WebView WebView { get; set; }
        WebClient webClient;

        private Xamarin.Forms.View CreateWebView()
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


            //WebView.NewWindowRequested += WebView_NewWindowRequested; ??
            WebView.FocusChange += WebView_FocusChange;

            var view = WebView.ToView();
            view.VerticalOptions = LayoutOptions.FillAndExpand;
            view.HorizontalOptions = LayoutOptions.FillAndExpand;
            return view;
        }

        private void WebView_FocusChange(object sender, Android.Views.View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
            {
                ReadingList.IsOnScreen = false;
                SettingsPane.IsOnScreen = false;
            }
        }


        private bool ShouldOverrideUrlLoading(WebView sender, Uri args)
        {
            jumpButton.IsEnabled = false;
            Title = "Loading...";

            var uri = Ao3SiteDataLookup.CheckUri(args);
            if (uri == null)
            {
                return true;
            }
            if (uri != args)
            {
                Navigate(uri);
                return true;
            }
            
            if (urlEntry != null) urlEntry.Text = args.AbsoluteUri;
            ReadingList?.PageChange(args);
            nextPage = null;
            prevPage = null;
            prevPageButton.IsEnabled = canGoBack;
            nextPageButton.IsEnabled = canGoForward;
            helper?.ClearEvents();
            return false;
        }

        private void OnPageStarted(WebView sender)
        {
            if (urlEntry != null) urlEntry.Text = WebView.Url;
            ReadingList?.PageChange(Current);
            WebView.AddJavascriptInterface(helper = new Ao3TrackHelper(this), "Ao3TrackHelper");
            PrevPageIndicator.IsVisible = false;
            NextPageIndicator.IsVisible = false;
            //WebView.RenderTransform = null;
            //WebView.Opacity = 1;
            helper?.ClearEvents();
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


        private void OnPageFinished(WebView sender)
        {
            var t = WebView.Title;

            t = t.EndsWith(" | Archive of Our Own") ? t.Substring(0, t.Length - 21) : t;
            t = t.EndsWith(" [Archive of Our Own]") ? t.Substring(0, t.Length - 21) : t;
            Title = t;

            // Inject JS script
            helper?.ClearEvents();
            EvaluateJavascript(JavaScriptInject);
        }

        public void EvaluateJavascript(string code)
        {
            DoOnMainThread(() => WebView.EvaluateJavascript(code, new ValueCallback((value) => { })));
        }
        public void CallJavascript(string function, params object[] args)
        {
            DoOnMainThread(() => WebView.EvaluateJavascript(function + "(" + string.Join(",", args) + ");", new ValueCallback((value) => { })));
        }

        public Uri Current
        {
            get
            {
                return DoOnMainThread(() => new Uri(WebView.Url));
            }
        }

        public void Navigate(Uri uri)
        {
            helper?.ClearEvents();
            WebView.LoadUrl(uri.AbsoluteUri);
        }

        public void Refresh()
        {
            WebView.Reload();
        }

        public bool canGoBack { get { return WebView.CanGoBack() || prevPage != null; } }

        public bool canGoForward { get { return WebView.CanGoForward() || nextPage != null; } }

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


        public double leftOffset
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

        public double opacity
        {
            get
            {
                return 1;
            }
            set
            {

            }
        }

        public Task<string> showContextMenu(double x, double y, [ReadOnlyArray] string[] menuItems)
        {
            return Task.Run(() => (string)null);
        }

        class WebClient : WebViewClient
        {
            WebViewPage page;

            public WebClient(WebViewPage page)
            {
                this.page = page;
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
                page.OnPageFinished(view);
                base.OnPageFinished(view, url);
            }

            public override void OnPageStarted(WebView view, string url, Bitmap favicon)
            {
                page.OnPageStarted(view);
                base.OnPageStarted(view, url, favicon);
            }

            [Obsolete]
            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                return page.ShouldOverrideUrlLoading(view, new Uri(url));
            }

            public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
            {
                return page.ShouldOverrideUrlLoading(view, new Uri(request.Url.ToString()));
            }
        }

        class ChromeClient : WebChromeClient
        {
            WebViewPage page;

            public ChromeClient(WebViewPage page)
            {
                this.page = page;
            }

            public override bool OnConsoleMessage(ConsoleMessage consoleMessage)
            {
                string message = consoleMessage.Message();
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