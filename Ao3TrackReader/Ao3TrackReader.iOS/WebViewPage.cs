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

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using WebKit;

using Ao3TrackReader.Helper;
using Ao3TrackReader.Data;
using System.IO;
using UIKit;
using CoreGraphics;
using ObjCRuntime;
using Foundation;
using Newtonsoft.Json;
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


        WKWebView webView;
        WKUserContentController userContentController;

        public static bool HaveOSBackButton { get; } = false;


        public Xamarin.Forms.View CreateWebView()
        {
            var preferences = new WKPreferences()
            {
                JavaScriptEnabled = true,
                JavaScriptCanOpenWindowsAutomatically = false
            };
            var configuration = new WKWebViewConfiguration()
            {
                UserContentController = userContentController = new WKUserContentController()
            };
            var helper = new Ao3TrackHelper(this); 
            userContentController.AddScriptMessageHandler(new ScriptMessageHandler(this, helper), "ao3track");
            configuration.Preferences = preferences;

            webView = new WKWebView(configuration)
            {
                NavigationDelegate = new NavigationDelegate(this)
            };
            this.helper = helper;

            webView.FocuseChanged += WebView_FocusChange;

            var xview = webView.ToView();
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
            var result = await webView.EvaluateJavaScriptAsync(code);
            return result?.ToString();
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
            return Task.CompletedTask;
        }

        async Task<string> ReadFile(string name, CancellationToken ct)
        {
            using (StreamReader sr = new StreamReader(Path.Combine(NSBundle.MainBundle.BundlePath, "Content", name), Encoding.UTF8))
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
                    if (!string.IsNullOrWhiteSpace(webView.Url?.AbsoluteString)) return new Uri(webView.Url.AbsoluteString);
                    else return new Uri("about:blank");
                });
            }
        }

        public void Navigate(Uri uri)
        {
            helper?.Reset();
            webView.LoadRequest(new Foundation.NSUrlRequest(new Foundation.NSUrl(uri.AbsoluteUri)));
        }

        public void Refresh()
        {
            webView.Reload();
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
                return webView.Frame.Width;
            }
        }

        public double LeftOffset
        {
            get
            {
                return webView.Transform.x0;
            }

            set
            {
                if (value == 0) webView.Transform = CGAffineTransform.MakeIdentity();
                else webView.Transform = CGAffineTransform.MakeTranslation(new nfloat(value), 0);
            }
        }

        public void CopyToClipboard(string str, string type)
        {

        }

        //Android.Widget.PopupMenu contextMenu = null;
        public void HideContextMenu()
        {
            /*
                contextMenu.Dismiss();
            */
        }

        public void ShowContextMenu(double x, double y, string url, string innerHtml)
        {
            HideContextMenu();

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

        class ScriptMessageHandler : WKScriptMessageHandler
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

            public override void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
            {
                var smsg = message.Body.ToString();
                var msg = JsonConvert.DeserializeObject<Message>(smsg);

                if (msg.type == "INIT")
                {
                    wvp.DoOnMainThread(async () =>
                    {
                        await wvp.EvaluateJavascriptAsync(string.Format(
                            "Ao3Track.iOS.helper.setValue({0},{1});Ao3Track.iOS.helper.setValue({2},{3});Ao3Track.iOS.helper.setValue({4},{5});Ao3Track.iOS.helper.setValue({6},{7});",
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