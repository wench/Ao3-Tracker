using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

#if WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;
using XPlatform = Xamarin.Forms.Platform.UWP.Platform;
#elif __WINDOWS__
using Xamarin.Forms.Platform.WinRT;
using XPlatform = Xamarin.Forms.Platform.WinRT.Platform;
#endif

namespace Ao3TrackReader.Controls
{
	public partial class LookupView : PaneView
	{
		public LookupView ()
		{
			InitializeComponent ();
		}

        protected override void OnIsOnScreenChanging(bool newValue)
        {
            base.OnIsOnScreenChanging(newValue);
            if (newValue == false) WebViewHolder.Content = null;
        }

        public void View(Uri uri, string title)
        {
            Title.Text = title;
            IsOnScreen = true;
            WebViewHolder.Content = new WebView();
            WebViewHolder.Content.Focus();
#if __WINDOWS__
            var renderer = (WebViewRenderer) XPlatform.GetRenderer(WebViewHolder.Content);

            var requestMessage = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, uri);
#if WINDOWS_UWP
            requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows Phone 10.0; Android 6.0.1; WebView/3.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Mobile Safari/537.36 Edge/15.15226");
            renderer.Control.Settings.IsIndexedDBEnabled = false;
            renderer.Control.Settings.IsJavaScriptEnabled = true;
#else
            requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Mobile; Windows Phone 8.1; Android 4.0; ARM; Trident/7.0; Touch; WebView/2.0; rv:11.0; IEMobile/11.0) like iPhone OS 7_0_3 Mac OS X AppleWebKit/537 (KHTML, like Gecko) Mobile Safari/537");
#endif

            renderer.Control.NavigateWithHttpRequestMessage(requestMessage);
            renderer.Control.Focus(Windows.UI.Xaml.FocusState.Programmatic);
#else
            WebViewHolder.Content.Source = uri;
#endif
        }
    }
}