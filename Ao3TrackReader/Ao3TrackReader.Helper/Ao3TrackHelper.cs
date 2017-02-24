using System;
using System.Collections.Generic;
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
    public interface IWorkChapter
    {
        long number { get; set; }
        long chapterid { get; set; }
        long? location { get; set; }
        long? seq { get; set; }
        bool IsNewer(IWorkChapter newitem);
        bool IsNewerOrSame(IWorkChapter newitem);

        long Paragraph { get; }
        long Frac { get; }
    }
    public interface IWorkChapterEx : IWorkChapter
    {
        long workid { get; set; }
        bool IsNewer(IWorkChapterEx newitem);
        bool IsNewerOrSame(IWorkChapterEx newitem);
    }

#if WINDOWS_UWP
    [AllowForWeb]
#endif
    public sealed class WorkChapter : IWorkChapterEx
    {
        public WorkChapter()
        {

        }
        //public WorkChapter(IWorkChapter toCopy)
        //{
        //    workid = 0;
        //    number = toCopy.number;
        //    chapterid = toCopy.chapterid;
        //    location = toCopy.location;
        //    seq = toCopy.seq;
        //}
        public WorkChapter(IWorkChapterEx toCopy)
        {
            workid = toCopy.workid; 
            number = toCopy.number;
            chapterid = toCopy.chapterid;
            location = toCopy.location;
            seq = toCopy.seq;
        }

        public long workid { get; set; }
        public long number { get; set; }
        public long chapterid { get; set; }
        public long? location { get; set; }
        public long? seq { get; set; }

        public long Paragraph {
            get {
                if (location == null) return long.MaxValue;
                return (long)location / 1000000000L;
            }
        }
        public long Frac {
            get {
                if (location == null) return long.MaxValue;
                var offset = (long)location % 1000000000L;
                if (offset == 500000000L) return 100;
                return offset * 100L / 479001600L;
            }
        }

        bool IWorkChapter.IsNewer(IWorkChapter newitem)
        {
            if (newitem.seq != null && this.seq != null)
            {
                if (newitem.seq > this.seq) { return true; }
                else if (newitem.seq < this.seq) { return false; }
            }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (this.location == null) { return false; }
            if (newitem.location == null) { return true; }

            return newitem.location > this.location;
        }
#if WINDOWS_UWP
        [DefaultOverload]
#endif
        public bool IsNewer(IWorkChapterEx newitem)
        {
            if (newitem.workid != workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.seq != null && this.seq != null)
            {
                if (newitem.seq > this.seq) { return true; }
                else if (newitem.seq < this.seq) { return false; }
            }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (this.location == null) { return false; }
            if (newitem.location == null) { return true; }

            return newitem.location > this.location;
        }
        bool IWorkChapter.IsNewerOrSame(IWorkChapter newitem)
        {
            if (newitem.seq != null && this.seq != null)
            {
                if (newitem.seq > this.seq) { return true; }
                else if (newitem.seq < this.seq) { return false; }
            }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (newitem.location == null) { return true; }
            if (this.location == null) { return false; }

            return newitem.location >= this.location;
        }
#if WINDOWS_UWP
        [DefaultOverload]
#endif
        public bool IsNewerOrSame(IWorkChapterEx newitem)
        {
            if (newitem.workid != workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.seq != null && this.seq != null)
            {
                if (newitem.seq > this.seq) { return true; }
                else if (newitem.seq < this.seq) { return false; }
            }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (newitem.location == null) { return true; }
            if (this.location == null) { return false; }

            return newitem.location >= this.location;
        }
    }

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

    public interface IEventHandler
    {
#if WINDOWS_UWP
        [DefaultOverload]
#endif
        object DoOnMainThread(MainThreadFunc function);
        void DoOnMainThread(MainThreadAction function);

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
        void AddToReadingList(string href);
        void SetCookies(string cookies);

        IAsyncOp_StringBoolMap AreUrlsInReadingListAsync([ReadOnlyArray] string[] urls);
    }
}
