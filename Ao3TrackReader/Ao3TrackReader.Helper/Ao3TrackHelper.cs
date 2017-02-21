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
        bool IsNewer(IWorkChapter newitem);
        bool IsNewerOrSame(IWorkChapter newitem);
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

            if (newitem.seq != null && this.seq != null)
            {
                if (this.location == null) { return false; }
                if (newitem.location == null) { return true; }
            }

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

#if WINDOWS_UWP
        IAsyncOperation<IDictionary<long, WorkChapter>> GetWorkChaptersAsync([ReadOnlyArray] long[] works);
#else
        Task<IDictionary<long, WorkChapter>> GetWorkChaptersAsync([ReadOnlyArray] long[] works);
#endif
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
#if WINDOWS_UWP
        IAsyncOperation<string> ShowContextMenu(double x, double y, [ReadOnlyArray] string[] menuItems);
#else
        Task<string> ShowContextMenu(double x, double y, [ReadOnlyArray] string[] menuItems);
#endif
        void AddToReadingList(string href);
        void SetCookies(string cookies);
    }
}
