using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ao3TrackReader
{
    // This is the stuff that the native part of the WebViewPage must implement
    interface IWebViewPageNative
    {
        bool IsMainThread { get; }

        bool ShowBackOnToolbar { get; }

        Xamarin.Forms.View CreateWebView();
        Uri CurrentUri { get; }
        void Navigate(Uri uri);
        void Refresh();
        bool CanGoBack { get; }
        bool CanGoForward { get; }
        void GoBack();
        void GoForward();
        string JavaScriptInject { get; }
        Task<string> EvaluateJavascriptAsync(string code);
        double LeftOffset { get; set; }

        void CloseContextMenu();
        Task<string> ShowContextMenu(double x, double y, string[] menuItems);
    }
}
