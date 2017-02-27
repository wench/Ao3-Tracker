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
        long Number { get; set; }
        long Chapterid { get; set; }
        long? Location { get; set; }
        long? Seq { get; set; }
        bool LessThan(IWorkChapter newitem);
        bool LessThanOrEqual(IWorkChapter newitem);

        long Paragraph { get; }
        long Frac { get; }
    }
    public interface IWorkChapterEx : IWorkChapter
    {
        long Workid { get; set; }
        bool LessThan(IWorkChapterEx newitem);
        bool LessThanOrEqual(IWorkChapterEx newitem);
    }

#if WINDOWS_UWP
    [AllowForWeb]
#endif
    public sealed class WorkChapter : IWorkChapterEx
    {
        public WorkChapter()
        {

        }

        public WorkChapter(IWorkChapterEx toCopy)
        {
            Workid = toCopy.Workid; 
            Number = toCopy.Number;
            Chapterid = toCopy.Chapterid;
            Location = toCopy.Location;
            Seq = toCopy.Seq;
        }

        public long Workid { get; set; }
        public long Number { get; set; }
        public long Chapterid { get; set; }
        public long? Location { get; set; }
        public long? Seq { get; set; }

        public long Paragraph {
            get {
                if (Location == null) return long.MaxValue;
                return (long)Location / 1000000000L;
            }
        }
        public long Frac {
            get {
                if (Location == null) return long.MaxValue;
                var offset = (long)Location % 1000000000L;
                if (offset == 500000000L) return 100;
                return offset * 100L / 479001600L;
            }
        }

        bool IWorkChapter.LessThan(IWorkChapter newitem)
        {
            if (newitem is IWorkChapterEx) return LessThan(newitem as IWorkChapterEx);

            if (newitem.Seq != null && this.Seq != null)
            {
                if (newitem.Seq > this.Seq) { return true; }
                else if (newitem.Seq < this.Seq) { return false; }
            }

            if (newitem.Number > this.Number) { return true; }
            else if (newitem.Number < this.Number) { return false; }

            if (this.Location == null) { return false; }
            if (newitem.Location == null) { return true; }

            return newitem.Location > this.Location;
        }
#if WINDOWS_UWP
        [DefaultOverload]
#endif
        public bool LessThan(IWorkChapterEx newitem)
        {
            if (newitem.Workid != Workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.Seq != null && this.Seq != null)
            {
                if (newitem.Seq > this.Seq) { return true; }
                else if (newitem.Seq < this.Seq) { return false; }
            }

            if (newitem.Number > this.Number) { return true; }
            else if (newitem.Number < this.Number) { return false; }

            if (this.Location == null) { return false; }
            if (newitem.Location == null) { return true; }

            return newitem.Location > this.Location;
        }
        bool IWorkChapter.LessThanOrEqual(IWorkChapter newitem)
        {
            if (newitem is IWorkChapterEx) return LessThanOrEqual(newitem as IWorkChapterEx);

            if (newitem.Seq != null && this.Seq != null)
            {
                if (newitem.Seq > this.Seq) { return true; }
                else if (newitem.Seq < this.Seq) { return false; }
            }

            if (newitem.Number > this.Number) { return true; }
            else if (newitem.Number < this.Number) { return false; }

            if (newitem.Location == null) { return true; }
            if (this.Location == null) { return false; }

            return newitem.Location >= this.Location;
        }
#if WINDOWS_UWP
        [DefaultOverload]
#endif
        public bool LessThanOrEqual(IWorkChapterEx newitem)
        {
            if (newitem.Workid != Workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.Seq != null && this.Seq != null)
            {
                if (newitem.Seq > this.Seq) { return true; }
                else if (newitem.Seq < this.Seq) { return false; }
            }

            if (newitem.Number > this.Number) { return true; }
            else if (newitem.Number < this.Number) { return false; }

            if (newitem.Location == null) { return true; }
            if (this.Location == null) { return false; }

            return newitem.Location >= this.Location;
        }

        //public static bool operator >=(WorkChapter left, WorkChapter right)
        //{
        //    return !left.LessThan(right);
        //}
        //public static bool operator <=(WorkChapter left, WorkChapter right)
        //{
        //    return left.LessThanOrEqual(right);
        //}
        //public static bool operator >=(WorkChapter left, IWorkChapter right)
        //{
        //    return !(left as IWorkChapter).LessThan(right);
        //}
        //public static bool operator <=(WorkChapter left, IWorkChapter right)
        //{
        //    return (left as IWorkChapter).LessThanOrEqual(right);
        //}
        //public static bool operator >=(IWorkChapter left, WorkChapter right)
        //{
        //    return (right as IWorkChapter).LessThanOrEqual(left);
        //}
        //public static bool operator <=(IWorkChapter left, WorkChapter right)
        //{
        //    return !(right as IWorkChapter).LessThan(left);
        //}
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

    public interface IWebViewPage
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

        void StartWebViewDragAccelerate(double velocity);
        void StopWebViewDragAccelerate();
    }
}
