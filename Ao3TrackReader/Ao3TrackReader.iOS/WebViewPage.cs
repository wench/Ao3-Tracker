using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using WebKit;

using Ao3TrackReader.Helper;
using Ao3TrackReader.Data;
using System.IO;
using UIKit;
using CoreGraphics;
using ObjCRuntime;

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
            get { return Foundation.NSThread.IsMain; }
        }

        public class WKWebView : WebKit.WKWebView
        {
            public WKWebView(WKWebViewConfiguration configuration) : base(new CoreGraphics.CGRect(0, 0, 360, 512), configuration)
            {
               
            }
            public event EventHandler<bool> FocuseChanged;

            public override bool BecomeFirstResponder()
            {
                bool ret = base.BecomeFirstResponder();
                if (ret) FocuseChanged?.Invoke(this, true);
                return ret;
            }

            public override bool ResignFirstResponder()
            {
                bool ret = base.ResignFirstResponder();
                if (ret) FocuseChanged?.Invoke(this, false);
                return ret;
            }
        }


        WKWebView WebView { get; set; }
        WKUserContentController userContentController;

        public bool ShowBackOnToolbar { get {
                return true;
            } }

        public Xamarin.Forms.View CreateWebView()
        {
            var preferences = new WKPreferences();
            preferences.JavaScriptEnabled = true;
            preferences.JavaScriptCanOpenWindowsAutomatically = false;

            var configuration = new WKWebViewConfiguration();
            configuration.UserContentController = userContentController = new WKUserContentController();
            configuration.Preferences = preferences;

            WebView = new WKWebView(configuration);
            WebView.NavigationDelegate = new NavigationDelegate(this);

            var h = new Ao3TrackHelper(this);
            helper = h;
            userContentController.AddScriptMessageHandler(h, "ao3track");

            WebView.FocuseChanged += WebView_FocusChange;
            var xview = WebView.ToView();
            xview.SizeChanged += Xview_SizeChanged;

            return xview;
        }

        private void Xview_SizeChanged(object sender, EventArgs e)
        {
            CallJavascriptAsync("Ao3Track.iOS.helper.setValue", "deviceWidth", DeviceWidth).Wait(0);
        }

        private void WebView_FocusChange(object sender, bool e)
        {
            if (e)
            {
                OnWebViewGotFocus();
            }
        }

        public async Task<string> EvaluateJavascriptAsync(string code)
        {
            var result = await WebView.EvaluateJavaScriptAsync(code);
            return result.ToString();
        }

        public void AddJavascriptObject(string name, object obj)
        {
            
        }

        void OnInjectingScripts()
        {
            EvaluateJavascriptAsync("window.Ao3TrackHelperNative = " + helper.MemberDef + ";").Wait();
        }

        void OnInjectedScripts()
        {
            CallJavascriptAsync("Ao3Track.iOS.helper.setValue", "leftOffset", LeftOffset).Wait(0);
            CallJavascriptAsync("Ao3Track.iOS.helper.setValue", "canGoBack", CanGoBack).Wait(0);
            CallJavascriptAsync("Ao3Track.iOS.helper.setValue", "canGoForward", CanGoForward).Wait(0);
            CallJavascriptAsync("Ao3Track.iOS.helper.setValue", "deviceWidth", DeviceWidth).Wait(0);
        }

        public Uri CurrentUri
        {
            get
            {
                return DoOnMainThread(() => new Uri(WebView.Url.AbsoluteString));
            }
        }

        public void Navigate(Uri uri)
        {
            helper?.Reset();
            WebView.LoadRequest(new Foundation.NSUrlRequest(new Foundation.NSUrl(uri.AbsoluteUri)));
        }

        public void Refresh()
        {
            WebView.Reload();
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
                return WebView.Frame.Width;
            }
        }

        public double LeftOffset
        {
            get
            {
                return WebView.Transform.x0;
            }

            set
            {
                if (value == 0) WebView.Transform = CGAffineTransform.MakeIdentity();
                else WebView.Transform = CGAffineTransform.MakeTranslation(new nfloat(value), 0);
            }
        }
        
        TaskCompletionSource<string> contextMenuResult = null;
        //Android.Widget.PopupMenu contextMenu = null;
        public void HideContextMenu()
        {
            if (contextMenuResult != null)
            {
                contextMenuResult.TrySetCanceled();
                contextMenuResult = null;
            }
            /*
            if (contextMenu != null)
            {
                contextMenu.Dismiss();
                contextMenu = null;
            }
            */
        }

        public Task<string> ShowContextMenu(double x, double y, string[] menuItems)
        {
            HideContextMenu();

            contextMenuResult = new TaskCompletionSource<string>();
            /*
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

            contextMenu.Show();*/
            contextMenuResult.SetResult("");
            return contextMenuResult.Task.ContinueWith((task) => {
                //contextMenu = null;
                contextMenuResult = null;
                return task.Result;
            });
        }

        class NavigationDelegate : WKNavigationDelegate
        {
            WebViewPage wvp;

            public NavigationDelegate(WebViewPage wvp)
            {
                this.wvp = wvp;
            }
        
            bool canDoOnContentLoaded = false;
            public override void DidFinishNavigation(WebKit.WKWebView webView, WKNavigation navigation)
            {
                if (canDoOnContentLoaded)
                {
                    wvp.OnContentLoaded();
                    canDoOnContentLoaded = false;
                }
            }

            public override void DidCommitNavigation(WebKit.WKWebView webView, WKNavigation navigation)
            {
                wvp.OnContentLoading();
                canDoOnContentLoaded = true;
            }

            public override void DecidePolicy(WebKit.WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
            {
                if (wvp.OnNavigationStarting(new Uri(navigationAction.Request.Url.AbsoluteString)))
                    decisionHandler(WKNavigationActionPolicy.Cancel);
                else
                    decisionHandler(WKNavigationActionPolicy.Allow);
            }
        }
    }

}