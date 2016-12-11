using System;
using Android.Webkit;
using Java.Interop;

namespace Ao3TrackReader.Droid
{
	public class JSBridge : Java.Lang.Object
	{
		readonly WeakReference<WebViewPage> hybridWebViewRenderer;

		public JSBridge (WebViewPage hybridRenderer)
		{
			hybridWebViewRenderer = new WeakReference <WebViewPage> (hybridRenderer);
		}

		[JavascriptInterface]
		[Export ("invokeAction")]
		public void InvokeAction (string data)
		{
			WebViewPage hybridRenderer;

			if (hybridWebViewRenderer != null && hybridWebViewRenderer.TryGetTarget (out hybridRenderer)) {
				//hybridRenderer.Element.InvokeAction (data);
			}
		}
	}
}

