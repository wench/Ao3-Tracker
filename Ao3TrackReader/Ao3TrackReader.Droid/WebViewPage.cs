using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading.Tasks;
//using Xamarin.Forms;
using Ao3TrackReader.Helper;

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

namespace Ao3TrackReader
{
    public partial class WebViewPage : IEventHandler
    {
        WebView WebView { get; set; }
        WebClient webClient;

        private Xamarin.Forms.View CreateWebView()
        {
            WebView = new WebView(Forms.Context);
            WebView.SetWebViewClient(webClient = new WebClient(this));
            //WebView.NewWindowRequested += WebView_NewWindowRequested; ??
            WebView.FocusChange += WebView_FocusChange;

            var view = WebView.ToView();
            view.VerticalOptions = LayoutOptions.FillAndExpand;
            view.HorizontalOptions = LayoutOptions.FillAndExpand;
            return view;
        }

        private void WebView_FocusChange(object sender, Android.Views.View.FocusChangeEventArgs e)
        {
            if (e.HasFocus) readingList.IsOnScreen = false;
        }


        private bool ShouldOverrideUrlLoading(WebView sender, Uri args)
        {
            jumpButton.IsEnabled = false;
            Title = "Loading...";

            if (args.Host == "archiveofourown.org" || args.Host == "www.archiveofourown.org")
            {
                if (args.Scheme == "http")
                {
                    var uri = new UriBuilder(args);
                    uri.Scheme = "https";
                    uri.Port = -1;
                    Navigate(uri.Uri);
                    return true;
                }

            }
            if (urlEntry != null) urlEntry.Text = args.AbsoluteUri;
            //WebView.AddWebAllowedObject("Ao3TrackHelper", helper = new Ao3TrackHelper(this));
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
            PrevPageIndicator.IsVisible = false;
            NextPageIndicator.IsVisible = false;
            //WebView.RenderTransform = null;
            //WebView.Opacity = 1;
            helper?.ClearEvents();
        }

        private void OnPageFinished(WebView sender)
        {
            var t = WebView.Title;

            t = t.EndsWith(" | Archive of Our Own") ? t.Substring(0, t.Length - 21) : t;
            t = t.EndsWith(" [Archive of Our Own]") ? t.Substring(0, t.Length - 21) : t;
            Title = t;

            // Inject JS script
            helper?.ClearEvents();
            Task<string> task = WebView.InvokeScriptAsync("eval", new[] { JavaScriptInject }).AsTask();
        }

        public Uri Current
        {
            get { return new Uri(WebView.Url); }
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


        public string[] cssToInject
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double leftOffset
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public double opacity
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string[] scriptsToInject
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Task<string> showContextMenu(double x, double y, [ReadOnlyArray] string[] menuItems)
        {
            throw new NotImplementedException();
        }

        class WebClient : WebViewClient
        {
            WebViewPage page;

            public WebClient(WebViewPage page)
            {
                this.page = page;
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

            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                return page.ShouldOverrideUrlLoading(view, new Uri(url));
            }
        }
    }

}