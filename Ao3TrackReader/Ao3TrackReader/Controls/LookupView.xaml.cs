using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Ao3TrackReader.Controls
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
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

        public void View(Uri uri)
        {
            IsOnScreen = true;
            WebViewHolder.Content = new WebView();
            WebViewHolder.Content.Focus();
#if __WINDOWS__
            var renderer = (Xamarin.Forms.Platform.UWP.WebViewRenderer) Xamarin.Forms.Platform.UWP.Platform.GetRenderer(WebViewHolder.Content);
            renderer.Control.Settings.IsIndexedDBEnabled = false;
            renderer.Control.Settings.IsJavaScriptEnabled = true;

            var requestMessage = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, uri);
            requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows Phone 10.0; Android 6.0.1; WebView/3.0; MicrosoftMDG; Lumia 650) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Mobile Safari/537.36 Edge/15.15226");
            renderer.Control.NavigateWithHttpRequestMessage(requestMessage);
            renderer.Control.Focus(Windows.UI.Xaml.FocusState.Programmatic);
#else
            WebViewHolder.Content.Source = uri;
#endif
        }
    }
}