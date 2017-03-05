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
    public sealed class Ao3TrackHelper : IAo3TrackHelper
    {
        static string s_memberDef;
        IWebViewPage wvp;
        public Ao3TrackHelper(IWebViewPage wvp)
        {
            this.wvp = wvp;
        }

        private T DoOnMainThread<T>(Func<T> func)
        {
            return (T)wvp.DoOnMainThread(() => func());
        }

        private void DoOnMainThread(Action func)
        {
            wvp.DoOnMainThread(() => func());
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

        void IAo3TrackHelper.Reset()
        {
            _JumpToLastLocationEvent = null;
            AlterFontSizeEvent = null;
        }


        private EventRegistrationTokenTable<EventHandler<bool>> _JumpToLastLocationEvent;
        public event EventHandler<bool> JumpToLastLocationEvent
        {
            add 
            {
                DoOnMainThread(() => { wvp.JumpToLastLocationEnabled = value != null; });

                return EventRegistrationTokenTable<EventHandler<bool>>
                    .GetOrCreateEventRegistrationTokenTable(ref _JumpToLastLocationEvent)
                    .AddEventHandler(value);
            }

            remove
            {
                EventRegistrationTokenTable<EventHandler<bool>>
                    .GetOrCreateEventRegistrationTokenTable(ref _JumpToLastLocationEvent)
                    .RemoveEventHandler(value);
            }
        }
        void IAo3TrackHelper.OnJumpToLastLocation(bool pagejump)
        {
            EventHandler<bool> temp =
                EventRegistrationTokenTable<EventHandler<bool>>
                .GetOrCreateEventRegistrationTokenTable(ref _JumpToLastLocationEvent)
                .InvocationList;

            temp?.Invoke(this, pagejump);
        }

        public event EventHandler<object> AlterFontSizeEvent;
        void IAo3TrackHelper.OnAlterFontSize(int fontSize)
        {
            Task<object>.Run(() =>
            {
                AlterFontSizeEvent?.Invoke(this, fontSize);
            });
        }

        internal static MemberDef md_GetWorkChaptersAsync = new MemberDef { @return = "WrapIMapNum" };
        public IAsyncOperation<object> GetWorkChaptersAsync([ReadOnlyArray] long[] works)
        {
            return Task.Run(async () =>
            {
                return (object)await wvp.GetWorkChaptersAsync(works);
            }).AsAsyncOperation();
        }

        internal static MemberDef md_CreateObject = null;
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

        public void HideContextMenu()
        {
            wvp.HideContextMenu();
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
            set { DoOnMainThread(() => { wvp.NextPage = value; }); }
        }
        public string PrevPage
        {
            get { return wvp.PrevPage ?? ""; }
            set { DoOnMainThread(() => { wvp.PrevPage = value; }); }
        }

        public bool CanGoBack
        {
            get { return DoOnMainThread(() => wvp.CanGoBack); }
        }
        public bool CanGoForward
        {
            get { return DoOnMainThread(() => wvp.CanGoForward); }
        }
        public void GoBack() { DoOnMainThread(() => wvp.GoBack()); }
        public void GoForward() { DoOnMainThread(() => wvp.GoForward()); }
        public double LeftOffset
        {
            get { return DoOnMainThread(() => wvp.LeftOffset); }
            set { DoOnMainThread(() => { wvp.LeftOffset = value; }); }
        }
        public int FontSizeMax { get { return wvp.FontSizeMax; } }
        public int FontSizeMin { get { return wvp.FontSizeMin; } }
        public int FontSize
        {
            get { return DoOnMainThread(() => wvp.FontSize); }
            set { DoOnMainThread(() => { wvp.FontSize = value; }); }
        }

        public int ShowPrevPageIndicator
        {
            get { return DoOnMainThread(() => wvp.ShowPrevPageIndicator); }
            set { DoOnMainThread(() => { wvp.ShowPrevPageIndicator = value; }); }
        }

        public int ShowNextPageIndicator
        {
            get { return DoOnMainThread(() => wvp.ShowNextPageIndicator); }
            set { DoOnMainThread(() => { wvp.ShowNextPageIndicator = value; }); }
        }

        internal static MemberDef md_CurrentLocation = new MemberDef { setter = "ToWorkChapterExNative" };
        public IWorkChapterEx CurrentLocation
        {
            get { return DoOnMainThread(() => wvp.CurrentLocation); }

            set { DoOnMainThread(() => { wvp.CurrentLocation = value; }); }
        }

        internal static MemberDef md_PageTitle = new MemberDef { setter = "ToPageTitleNative" };
        public PageTitle PageTitle
        {
            get { return DoOnMainThread(() => wvp.PageTitle); }

            set { DoOnMainThread(() => { wvp.PageTitle = value; }); }
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
            DoOnMainThread(() => wvp.StartWebViewDragAccelerate(velocity));
        }

        public void StopWebViewDragAccelerate()
        {
            wvp.StopWebViewDragAccelerate();
        }

        public double DeviceWidth
        {
            get
            {
                return DoOnMainThread(()=>wvp.DeviceWidth);
            }
        }
    }
}
