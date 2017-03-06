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

        static HelperDef s_def;
        static HelperDef HelperDef
        {
            get
            {
                if (s_def == null)
                {
                    s_def = new HelperDef();
                    s_def.FillFromType(typeof(Ao3TrackHelper));
                }
                return s_def;
            }
        }

        static string s_helperDefJson;
        internal static MemberDef md_HelperDefJson = null;
        public string HelperDefJson
        {
            get
            {
                if (s_helperDefJson == null) s_helperDefJson = HelperDef.Serialize();
                return s_helperDefJson;
            }
        }

        void IAo3TrackHelper.Reset()
        {
            _JumpToLastLocationEvent = null;
            _AlterFontSizeEvent = null;
        }

        private EventRegistrationTokenTable<EventHandler<bool>> _JumpToLastLocationEvent;
        public event EventHandler<bool> JumpToLastLocationEvent
        {
            add
            {
                DoOnMainThread(() => { wvp.JumpToLastLocationEnabled = true; });

                return EventRegistrationTokenTable<EventHandler<bool>>
                    .GetOrCreateEventRegistrationTokenTable(ref _JumpToLastLocationEvent)
                    .AddEventHandler(value);
            }

            remove
            {
                DoOnMainThread(() => { wvp.JumpToLastLocationEnabled = false; });

                EventRegistrationTokenTable<EventHandler<bool>>
                    .GetOrCreateEventRegistrationTokenTable(ref _JumpToLastLocationEvent)
                    .RemoveEventHandler(value);
            }
        }
        void IAo3TrackHelper.OnJumpToLastLocation(bool pagejump)
        {
            var handlers =
                EventRegistrationTokenTable<EventHandler<bool>>
                .GetOrCreateEventRegistrationTokenTable(ref _JumpToLastLocationEvent)
                .InvocationList;

            handlers?.Invoke(this, pagejump);
        }

        private EventRegistrationTokenTable<EventHandler<int>> _AlterFontSizeEvent;
        public event EventHandler<int> AlterFontSizeEvent
        {
            add
            {
                value.Invoke(this, wvp.FontSize);

                return EventRegistrationTokenTable<EventHandler<int>>
                    .GetOrCreateEventRegistrationTokenTable(ref _AlterFontSizeEvent)
                    .AddEventHandler(value);
            }

            remove
            {
                EventRegistrationTokenTable<EventHandler<int>>
                    .GetOrCreateEventRegistrationTokenTable(ref _AlterFontSizeEvent)
                    .RemoveEventHandler(value);
            }
        }
        void IAo3TrackHelper.OnAlterFontSize(int fontSize)
        {
            var handlers =
                EventRegistrationTokenTable<EventHandler<int>>
                .GetOrCreateEventRegistrationTokenTable(ref _AlterFontSizeEvent)
                .InvocationList;

            handlers?.Invoke(this, fontSize);
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

        internal static MemberDef md_NextPage = new MemberDef { getter = "false" };
        public string NextPage
        {
            get { throw new NotSupportedException(); }
            set { DoOnMainThread(() => { wvp.NextPage = value; }); }
        }

        internal static MemberDef md_PrevPage = new MemberDef { getter = "false" };
        public string PrevPage
        {
            get { throw new NotSupportedException(); }
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

        internal static MemberDef md_ShowPrevPageIndicator = new MemberDef { getter = "false" };
        public int ShowPrevPageIndicator
        {
            get { throw new NotSupportedException(); }
            set { DoOnMainThread(() => { wvp.ShowPrevPageIndicator = value; }); }
        }

        internal static MemberDef md_ShowNextPageIndicator = new MemberDef { getter = "false" };
        public int ShowNextPageIndicator
        {
            get { throw new NotSupportedException(); }
            set { DoOnMainThread(() => { wvp.ShowNextPageIndicator = value; }); }
        }

        internal static MemberDef md_CurrentLocation = new MemberDef { getter = "false", setter = "ToWorkChapterExNative" };
        public IWorkChapterEx CurrentLocation
        {
            get { throw new NotSupportedException(); }
            set { DoOnMainThread(() => { wvp.CurrentLocation = value; }); }
        }

        internal static MemberDef md_PageTitle = new MemberDef { getter = "false", setter = "ToPageTitleNative" };
        public PageTitle PageTitle
        {
            get { throw new NotSupportedException(); }
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
                return DoOnMainThread(() => wvp.DeviceWidth);
            }
        }
    }
}
