using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;

#if WINDOWS_UWP
using Windows.Foundation;
using Windows.Foundation.Metadata;
using IAsyncOp_WorkChapterMap = Windows.Foundation.IAsyncOperation<System.Collections.Generic.IDictionary<long, Ao3TrackReader.Helper.WorkChapter>>;
using IAsyncOp_StringBoolMap = Windows.Foundation.IAsyncOperation<System.Collections.Generic.IDictionary<string, bool>>;
using IAsyncOp_String = Windows.Foundation.IAsyncOperation<string>;
#else 
using System.Threading.Tasks;
using IAsyncOperation = System.Threading.Tasks.Task;
using IAsyncOp_WorkChapterMap = System.Threading.Tasks.Task<System.Collections.Generic.IDictionary<long, Ao3TrackReader.Helper.WorkChapter>>;
using IAsyncOp_StringBoolMap = System.Threading.Tasks.Task<System.Collections.Generic.IDictionary<string, bool>>;
using IAsyncOp_String = System.Threading.Tasks.Task<string>;
#endif


namespace Ao3TrackReader.Helper
{
#if WINDOWS_UWP
    [AllowForWeb]
#endif
    public sealed class PageTitle
    {
        public string Title { get; set; }
        public string Chapter { get; set; }
        public string Chaptername { get; set; }
        public string[] Authors { get; set; }
        public string[] Fandoms { get; set; }
        public string Primarytag { get; set; }
    }


    public delegate void MainThreadAction();
    public delegate object MainThreadFunc();

    public interface IWebViewPage
    {
#if WINDOWS_UWP
        [DefaultOverload]
#endif
        object DoOnMainThread(MainThreadFunc function);
        void DoOnMainThread(MainThreadAction function);

        double DeviceWidth { get; }

        IAsyncOp_WorkChapterMap GetWorkChaptersAsync([ReadOnlyArray] long[] works);
        void SetWorkChapters(IDictionary<long, WorkChapter> works);
        bool JumpToLastLocationEnabled { get; set; }
        string NextPage { get; set; }
        string PrevPage { get; set; }
        bool CanGoBack { get; }
        bool CanGoForward { get; }
        void GoBack();
        void GoForward();
        double LeftOffset { get; set; }
        int ShowPrevPageIndicator { get; set; }
        int ShowNextPageIndicator { get; set; }
        string[] ScriptsToInject { get; }
        string[] CssToInject { get; }
        int FontSizeMax { get; }
        int FontSizeMin { get; }
        int FontSize { get; set; }
        IWorkChapterEx CurrentLocation { get; set; }
        PageTitle PageTitle { get; set; }
        IAsyncOp_String ShowContextMenu(double x, double y, [ReadOnlyArray] string[] menuItems);
        void HideContextMenu();
        void AddToReadingList(string href);
        void SetCookies(string cookies);

        IAsyncOp_StringBoolMap AreUrlsInReadingListAsync([ReadOnlyArray] string[] urls);

        void StartWebViewDragAccelerate(double velocity);
        void StopWebViewDragAccelerate();

        IAsyncOp_String CallJavascriptAsync(string function, params object[] args);
        IAsyncOp_String EvaluateJavascriptAsync(string code);
    }
}
