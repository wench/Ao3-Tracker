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
        static string s_memberDef;

        IWebViewPage wvp;
        public Ao3TrackHelper(IWebViewPage wvp)
        {
            this.wvp = wvp;
        }

        internal static MemberDef md_ScriptsToInject = null;
        public string[] ScriptsToInject { get { return wvp.ScriptsToInject; } }

        internal static MemberDef md_CssToInject = null;
        public string[] CssToInject { get { return wvp.CssToInject; } }

        internal static MemberDef md_MemberDef = null;
        public string MemberDef
        {
            get
            {
                if (s_memberDef == null)
                {
                    var def = new HelperDef();
                    def.FillFromType(typeof(Ao3TrackHelper));
                    s_memberDef = def.Serialize();
                }
                return s_memberDef;
            }
        }

        public event EventHandler<bool> JumpToLastLocationEvent;

        internal static MemberDef md_OnJumpToLastLocation = null;
        public void OnJumpToLastLocation(bool pagejump)
        {
            Task<object>.Run(() =>
            {
                JumpToLastLocationEvent?.Invoke(this, pagejump);
            });
        }

        public bool jumpToLastLocationEnabled
        {
            get { return (bool)wvp.DoOnMainThread(() => wvp.JumpToLastLocationEnabled); }
            set { wvp.DoOnMainThread(() => { wvp.JumpToLastLocationEnabled = value; }); }
        }


        public event EventHandler<object> AlterFontSizeEvent;

        internal static MemberDef md_OnAlterFontSize = null;
        public void OnAlterFontSize()
        {
            Task<object>.Run(() =>
            {
                AlterFontSizeEvent?.Invoke(this, null);
            });
        }

        internal static MemberDef md_Reset = null;
        public void Reset()
        {
            JumpToLastLocationEvent = null;
            AlterFontSizeEvent = null;
        }

        internal static MemberDef md_GetWorkChaptersAsync = new MemberDef { @return = "WrapIMapNum" };
        public IAsyncOperation<object> GetWorkChaptersAsync([ReadOnlyArray] long[] works)
        {
            return Task.Run(async () =>
            {
                return (object)await wvp.GetWorkChaptersAsync(works);
            }).AsAsyncOperation();
        }

        public object CreateObject(string classname)
        {
            switch (classname)
            {
                case "WorkChapterNative":
                    return new WorkChapter();

                case "WorkChapterExNative":
                    return new WorkChapter();

                case "WorkChapterMapNative":
                    return new Dictionary<long, WorkChapter>();

                case "PageTitleNative":
                    return new PageTitle();
            }

            return null;
        }

        internal static MemberDef md_SetWorkChapters = new MemberDef { args = new Dictionary<int, string> { { 0, "ToWorkChapterMapNative" } } };
        public void SetWorkChapters(IDictionary<long, WorkChapter> works)
        {
            Task.Run(() =>
            {
                wvp.SetWorkChapters(works);
            });
        }

        public IAsyncOperation<string> ShowContextMenu(double x, double y, [ReadOnlyArray] string[] menuItems)
        {
            return wvp.ShowContextMenu(x, y, menuItems);
        }

        public void AddToReadingList(string href)
        {
            wvp.AddToReadingList(href);
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
                dp.SetText(str);
                dp.SetWebLink(new Uri(str));
                Clipboard.SetContent(dp);
            }
        }
        public void SetCookies(string cookies)
        {
            wvp.SetCookies(cookies);
        }

        public string NextPage
        {
            get { return wvp.NextPage ?? ""; }
            set { wvp.DoOnMainThread(() => { wvp.NextPage = value; }); }
        }
        public string PrevPage
        {
            get { return wvp.PrevPage ?? ""; }
            set { wvp.DoOnMainThread(() => { wvp.PrevPage = value; }); }
        }

        public bool CanGoBack
        {
            get { return (bool)wvp.DoOnMainThread(() => wvp.CanGoBack); }
        }
        public bool CanGoForward
        {
            get { return (bool)wvp.DoOnMainThread(() => wvp.CanGoForward); }
        }
        public void GoBack() { wvp.DoOnMainThread(() => wvp.GoBack()); }
        public void GoForward() { wvp.DoOnMainThread(() => wvp.GoForward()); }
        public double LeftOffset
        {
            get { return (double)wvp.DoOnMainThread(() => wvp.LeftOffset); }
            set { wvp.DoOnMainThread(() => { wvp.LeftOffset = value; }); }
        }
        public int FontSizeMax { get { return wvp.FontSizeMax; } }
        public int FontSizeMin { get { return wvp.FontSizeMin; } }
        public int FontSize
        {
            get { return (int)wvp.DoOnMainThread(() => wvp.FontSize); }
            set { wvp.DoOnMainThread(() => { wvp.FontSize = value; }); }
        }

        public int ShowPrevPageIndicator
        {
            get { return (int)wvp.DoOnMainThread(() => wvp.ShowPrevPageIndicator); }
            set { wvp.DoOnMainThread(() => { wvp.ShowPrevPageIndicator = value; }); }
        }

        public int ShowNextPageIndicator
        {
            get { return (int)wvp.DoOnMainThread(() => wvp.ShowNextPageIndicator); }
            set { wvp.DoOnMainThread(() => { wvp.ShowNextPageIndicator = value; }); }
        }

        internal static MemberDef md_CurrentLocation = new MemberDef { setter = "ToWorkChapterExNative" };
        public IWorkChapterEx CurrentLocation
        {
            get { return (IWorkChapterEx)wvp.DoOnMainThread(() => wvp.CurrentLocation); }

            set { wvp.DoOnMainThread(() => { wvp.CurrentLocation = value; }); }
        }

        internal static MemberDef md_PageTitle = new MemberDef { setter = "ToPageTitleNative" };
        public PageTitle PageTitle
        {
            get { return (PageTitle)wvp.DoOnMainThread(() => wvp.PageTitle); }

            set { wvp.DoOnMainThread(() => { wvp.PageTitle = value; }); }
        }

        internal static MemberDef md_AreUrlsInReadingListAsync = new MemberDef { @return = "WrapIMapString" };
        public IAsyncOperation<object> AreUrlsInReadingListAsync([ReadOnlyArray] string[] urls)
        {
            return Task.Run(async () =>
            {
                return (object)await wvp.AreUrlsInReadingListAsync(urls);
            }).AsAsyncOperation();
        }

        public void StartWebViewDragAccelerate(double velocity)
        {
            wvp.DoOnMainThread(() => wvp.StartWebViewDragAccelerate(velocity));
        }

        public void StopWebViewDragAccelerate()
        {
            wvp.StopWebViewDragAccelerate();
        }

    }
}
