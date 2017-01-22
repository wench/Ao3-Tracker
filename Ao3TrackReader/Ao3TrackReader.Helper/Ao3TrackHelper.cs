using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;

#if WINDOWS_UWP
using Windows.Foundation;
using Windows.Foundation.Metadata;
#else 
using System.Threading.Tasks;
#endif

namespace Ao3TrackReader.Helper
{
    public interface IWorkChapter
    {
        long number { get; set; }
        long chapterid { get; set; }
        long? location { get; set; }
        long? seq { get; set; }
    }

#if WINDOWS_UWP
    [AllowForWeb]
#endif
    public sealed class WorkChapter : IWorkChapter
    {
        public WorkChapter()
        {

        }
        public WorkChapter(IWorkChapter toCopy)
        {
            number = toCopy.number;
            chapterid = toCopy.chapterid;
            location = toCopy.location;
            seq = toCopy.seq;
        }

        public long number { get; set; }
        public long chapterid { get; set; }
        public long? location { get; set; }
        public long? seq { get; set; }
    }

    public delegate void MainThreadAction();
    public delegate object MainThreadFunc();

    public interface IEventHandler
    {
#if WINDOWS_UWP
        [DefaultOverload]
#endif
        object DoOnMainThread(MainThreadFunc function);
        void DoOnMainThread(MainThreadAction function);

#if WINDOWS_UWP
        IAsyncOperation<IDictionary<long, WorkChapter>> GetWorkChaptersAsync([ReadOnlyArray] long[] works);
#else
        Task<IDictionary<long, WorkChapter>> GetWorkChaptersAsync([ReadOnlyArray] long[] works);
#endif
        void SetWorkChapters(IDictionary<long, WorkChapter> works);
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

#if WINDOWS_UWP
        IAsyncOperation<string> showContextMenu(double x, double y, [ReadOnlyArray] string[] menuItems);
#else
        Task<string> showContextMenu(double x, double y, [ReadOnlyArray] string[] menuItems);
#endif
        void addToReadingList(string href);
        void setCookies(string cookies);
    }
}
