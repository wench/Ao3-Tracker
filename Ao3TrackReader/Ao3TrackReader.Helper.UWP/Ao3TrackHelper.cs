using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;

namespace Ao3TrackReader.Helper
{
    public interface IWorkChapter
    {
        long number { get; set; }
        long chapterid { get; set; }
        long? location { get; set; }
    }

    [AllowForWeb]
    public sealed class WorkChapter : IWorkChapter
    {
        public long number { get; set; }
        public long chapterid { get; set; }
        public long? location { get; set; }
    }

    public interface IEventHandler
    {
        IDictionary<long, IWorkChapter> GetWorkChapters([ReadOnlyArray] long[] works);

        void SetWorkChapters(IDictionary<long, IWorkChapter> works);

        void EnableJumpToLastLocation(bool enable);

        bool canGoBack { get; }
        bool canGoForward { get; }
        double leftOffset { get; set; }
        double opacity { get; set; }

    }

    [AllowForWeb]
    public sealed class Ao3TrackHelper
    {
        IEventHandler handler;
        private CoreDispatcher m_dispatcher;

        public event EventHandler<bool> JumpToLastLocationEvent;

        public void JumpToLastLocation(bool pagejump)
        {
            Task<object>.Run(() =>
            {
                JumpToLastLocationEvent?.Invoke(this, pagejump);
            });
        }

        public void EnableJumpToLastLocation(bool enable)
        {
            handler.EnableJumpToLastLocation(enable);
        }

        public Ao3TrackHelper(IEventHandler handler)
        {
            var window = Windows.UI.Core.CoreWindow.GetForCurrentThread();
            m_dispatcher = window.Dispatcher;

            this.handler = handler;
        }

        public IAsyncOperation<object> GetWorkChaptersAsync([ReadOnlyArray] long[] works)
        {
            return Task<object>.Run(() =>
            {
                return handler.GetWorkChapters(works) as object;
            }).AsAsyncOperation();           
        }

        public IDictionary<long, IWorkChapter> createWorkChapterMap()
        {
            return new Dictionary<long, IWorkChapter>();
        }

        public IWorkChapter createWorkChapter(long number, long chapterid, long? location)
        {
            return new WorkChapter {
                number = number,
                chapterid = chapterid,
                location = location
            };

        }

        public void SetWorkChapters(IDictionary<long, IWorkChapter> works)
        {
            Task.Run(() => 
            {
                handler.SetWorkChapters(works);
            });
        }

        public bool canGoBack { get { return handler.canGoBack; } }
        public bool canGoForward { get { return handler.canGoForward; } }
        public double leftOffset { get { return handler.leftOffset; } set { handler.leftOffset = value; } }
        public double opacity {  get { return handler.opacity; } set { handler.opacity = value; } }

        public event EventHandler<object> AlterFontSizeEvent;
        private int font_size = 100;
        public int FontSizeMax { get { return 300; } }
        public int FontSizeMin { get { return 10; } }
        public int FontSize
        {
            get
            {
                return font_size;
            }
            set
            {
                font_size = value;
                if (font_size < FontSizeMin) font_size = FontSizeMin;
                else if (font_size > FontSizeMax) font_size = FontSizeMax;
                Task<object>.Run(() =>
                {
                    AlterFontSizeEvent?.Invoke(this, null);
                });
            }
        }


    }
}
