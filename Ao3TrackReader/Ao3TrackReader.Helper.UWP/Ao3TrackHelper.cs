using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.ApplicationModel.DataTransfer;

namespace Ao3TrackReader.Helper
{
    [AllowForWeb]
    public sealed class WorkChapter : IWorkChapter
    {
        public long number { get; set; }
        public long chapterid { get; set; }
        public long? location { get; set; }
    }

    [AllowForWeb]
    public sealed class Ao3TrackHelper
    {
        IEventHandler handler;
        public Ao3TrackHelper(IEventHandler handler)
        {
            this.handler = handler;
        }

        public string[] scriptsToInject { get { return handler.scriptsToInject; } }
        public string[] cssToInject { get { return handler.cssToInject; } }


        public event EventHandler<bool> JumpToLastLocationEvent;
        public void OnJumpToLastLocation(bool pagejump)
        {
            Task<object>.Run(() =>
            {
                JumpToLastLocationEvent?.Invoke(this, pagejump);
            });
        }

        public event EventHandler<object> AlterFontSizeEvent;
        public void OnAlterFontSizeEvent()
        {
            Task<object>.Run(() =>
            {
                AlterFontSizeEvent?.Invoke(this, null);
            });
        }
        public void ClearEvents()
        {
            JumpToLastLocationEvent = null;
            AlterFontSizeEvent = null;
        }

        public IAsyncOperation<object> getWorkChaptersAsync([ReadOnlyArray] long[] works)
        {
            return Task.Run(async () =>
            {
                return (object)await handler.GetWorkChaptersAsync(works);
            }).AsAsyncOperation();
        }

        public IDictionary<long, IWorkChapter> createWorkChapterMap()
        {
            return new Dictionary<long, IWorkChapter>();
        }

        public IWorkChapter createWorkChapter(long number, long chapterid, long? location)
        {
            return new WorkChapter
            {
                number = number,
                chapterid = chapterid,
                location = location
            };

        }

        public void setWorkChapters(IDictionary<long, IWorkChapter> works)
        {
            Task.Run(() =>
            {
                handler.SetWorkChapters(works);
            });
        }

        public bool jumpToLastLocationEnabled
        {
            get { return (bool)handler.DoOnMainThread(() => handler.JumpToLastLocationEnabled); }
            set { handler.DoOnMainThread(() => handler.JumpToLastLocationEnabled = value); }
        }
        public string nextPage
        {
            get { return handler.NextPage; }
            set { handler.DoOnMainThread(() => handler.NextPage = value); }
        }
        public string prevPage
        {
            get { return handler.PrevPage; }
            set { handler.DoOnMainThread(() => handler.PrevPage = value); }
        }

        public bool canGoBack
        {
            get { return (bool)handler.DoOnMainThread(() => handler.canGoBack); }
        }
        public bool canGoForward
        {
            get { return (bool)handler.DoOnMainThread(() => handler.canGoForward); }
        }
        public void goBack() { handler.DoOnMainThread(() => handler.GoBack()); }
        public void goForward() { handler.DoOnMainThread(() => handler.GoForward()); }
        public double leftOffset
        {
            get { return (double)handler.DoOnMainThread(() => handler.leftOffset); }
            set { handler.DoOnMainThread(() => handler.leftOffset = value); }
        }
        public double opacity
        {
            get { return (double)handler.DoOnMainThread(() => handler.opacity); }
            set { handler.DoOnMainThread(() => handler.opacity = value); }
        }
        public int fontSizeMax { get { return handler.FontSizeMax; } }
        public int fontSizeMin { get { return handler.FontSizeMin; } }
        public int fontSize
        {
            get { return (int)handler.DoOnMainThread(() => handler.FontSize); }
            set { handler.DoOnMainThread(() => handler.FontSize = value); }
        }
        public bool showPrevPageIndicator
        {
            get { return (bool)handler.DoOnMainThread(() => handler.showPrevPageIndicator); }
            set { handler.DoOnMainThread(() => handler.showPrevPageIndicator = value); }
        }
        public bool showNextPageIndicator
        {
            get { return (bool)handler.DoOnMainThread(() => handler.showNextPageIndicator); }
            set { handler.DoOnMainThread(() => handler.showNextPageIndicator = value); }
        }

        public IAsyncOperation<string> showContextMenu(double x, double y, [ReadOnlyArray] string[] menuItems)
        {
            return handler.showContextMenu(x, y, menuItems);
        }

        public void addToReadingList(string href)
        {
            handler.addToReadingList(href);
        }
        public void copyToClipboard(string str, string type)
        {
            if (type == "text" || type == "uri")
            {
                var dp = new DataPackage();
                dp.SetText(str);
                Clipboard.SetContent(dp);
            }
            else if (type == "uri")
            {
                var dp = new DataPackage();
                dp.SetWebLink(new Uri(str));
                Clipboard.SetContent(dp);
            }
        }
        public void setCookies(string cookies)
        {
            handler.setCookies(cookies);
        }
    }
}
