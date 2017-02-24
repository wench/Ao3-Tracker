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
    public delegate IWorkChapter GetCurrentLocationDel();

    [AllowForWeb]
    public sealed class Ao3TrackHelper
    {
        IEventHandler handler;
        public Ao3TrackHelper(IEventHandler handler)
        {
            this.handler = handler;
        }

        public string[] ScriptsToInject { get { return handler.ScriptsToInject; } }
        public string[] CssToInject { get { return handler.CssToInject; } }


        public event EventHandler<bool> JumpToLastLocationEvent;
        public void OnJumpToLastLocation(bool pagejump)
        {
            Task<object>.Run(() =>
            {
                JumpToLastLocationEvent?.Invoke(this, pagejump);
            });
        }
        public bool jumpToLastLocationEnabled
        {
            get { return (bool)handler.DoOnMainThread(() => handler.JumpToLastLocationEnabled); }
            set { handler.DoOnMainThread(() => { handler.JumpToLastLocationEnabled = value; }); }
        }


        public event EventHandler<object> AlterFontSizeEvent;
        public void OnAlterFontSize()
        {
            Task<object>.Run(() =>
            {
                AlterFontSizeEvent?.Invoke(this, null);
            });
        }
        public void Reset()
        {
            JumpToLastLocationEvent = null;
            AlterFontSizeEvent = null;
        }

        public IAsyncOperation<object> GetWorkChaptersAsync([ReadOnlyArray] long[] works)
        {
            return Task.Run(async () =>
            {
                return (object)await handler.GetWorkChaptersAsync(works);
            }).AsAsyncOperation();
        }

        public object CreateObject(string classname)
        {
            switch (classname)
            {
                case "WorkChapter":
                    return new WorkChapter();

                case "WorkChapterEx":
                    return new WorkChapter();

                case "WorkChapterMap":
                    return new Dictionary<long, WorkChapter>();

                case "PageTitle":
                    return new PageTitle();
            }

            return null;
        }
        public void SetWorkChapters(IDictionary<long, WorkChapter> works)
        {
            Task.Run(() =>
            {
                handler.SetWorkChapters(works);
            });
        }

        public IAsyncOperation<string> ShowContextMenu(double x, double y, [ReadOnlyArray] string[] menuItems)
        {
            return handler.ShowContextMenu(x, y, menuItems);
        }

        public void AddToReadingList(string href)
        {
            handler.AddToReadingList(href);
        }
        public void CopyToClipboard(string str, string type)
        {
            if (type == "text")
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
        public void SetCookies(string cookies)
        {
            handler.SetCookies(cookies);
        }

        public string NextPage
        {
            get { return handler.NextPage; }
            set { handler.DoOnMainThread(() => { handler.NextPage = value; }); }
        }
        public string PrevPage
        {
            get { return handler.PrevPage; }
            set { handler.DoOnMainThread(() => { handler.PrevPage = value; }); }
        }

        public bool CanGoBack
        {
            get { return (bool)handler.DoOnMainThread(() => handler.CanGoBack); }
        }
        public bool CanGoForward
        {
            get { return (bool)handler.DoOnMainThread(() => handler.CanGoForward); }
        }
        public void GoBack() { handler.DoOnMainThread(() => handler.GoBack()); }
        public void GoForward() { handler.DoOnMainThread(() => handler.GoForward()); }
        public double LeftOffset
        {
            get { return (double)handler.DoOnMainThread(() => handler.LeftOffset); }
            set { handler.DoOnMainThread(() => { handler.LeftOffset = value; }); }
        }
        public int FontSizeMax { get { return handler.FontSizeMax; } }
        public int FontSizeMin { get { return handler.FontSizeMin; } }
        public int FontSize
        {
            get { return (int)handler.DoOnMainThread(() => handler.FontSize); }
            set { handler.DoOnMainThread(() => { handler.FontSize = value; }); }
        }
        public int ShowPrevPageIndicator
        {
            get { return (int)handler.DoOnMainThread(() => handler.ShowPrevPageIndicator); }
            set { handler.DoOnMainThread(() => { handler.ShowPrevPageIndicator = value; }); }
        }
        public int ShowNextPageIndicator
        {
            get { return (int)handler.DoOnMainThread(() => handler.ShowNextPageIndicator); }
            set { handler.DoOnMainThread(() => { handler.ShowNextPageIndicator = value; }); }
        }

        public IWorkChapterEx CurrentLocation { get { return (IWorkChapterEx) handler.DoOnMainThread(() => handler.CurrentLocation); } set { handler.DoOnMainThread(() => { handler.CurrentLocation = value; }); } }
        public PageTitle PageTitle { get { return (PageTitle) handler.DoOnMainThread(() => handler.PageTitle); } set { handler.DoOnMainThread(() => { handler.PageTitle = value; }); } }

        public IAsyncOperation<IDictionary<string, bool>> AreUrlsInReadingListAsync([ReadOnlyArray] string[] urls)
        {
            return handler.AreUrlsInReadingListAsync(urls);
        }
    }
}
