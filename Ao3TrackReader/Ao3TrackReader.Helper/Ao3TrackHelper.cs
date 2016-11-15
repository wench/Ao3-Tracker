using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Ao3TrackReader.Helper
{
    public interface IWorkChapter
    {
        long number { get; set; }
        long chapterid { get; set; }
        long? location { get; set; }
    }

    public delegate void MainThreadAction();
    public delegate object MainThreadFunc();

    public interface IEventHandler
    {
        [DefaultOverload]
        object DoOnMainThread(MainThreadFunc function);
        void DoOnMainThread(MainThreadAction function);

        IAsyncOperation<IDictionary<long, IWorkChapter>> GetWorkChaptersAsync([ReadOnlyArray] long[] works);
        void SetWorkChapters(IDictionary<long, IWorkChapter> works);
        bool JumpToLastLocationEnabled { get; set; }
        string NextPage { get; set; }
        string PrevPage { get; set; }
        bool canGoBack { get; }
        bool canGoForward { get; }
        void GoBack();
        void GoForward();
        double leftOffset { get; set; }
        double opacity { get; set; }
        bool showPrevPageIndicator { get; set; }
        bool showNextPageIndicator { get; set; }
        string[] scriptsToInject { get; }
        string[] cssToInject { get; }
        int FontSizeMax { get; }
        int FontSizeMin { get; }
        int FontSize { get; set; }
        double realWidth { get; }
        double realHeight { get; }

        IAsyncOperation<string> showContextMenu(double x, double y, [ReadOnlyArray] string[] menuItems);
        void addToReadingList(string href);
    }
}
