/*
Copyright 2017 Alexis Ryan

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.ApplicationModel.DataTransfer;

using Newtonsoft.Json;

namespace Ao3TrackReader.Helper
{
#if WINDOWS_UWP
    [AllowForWeb]
#endif
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
        internal static MemberDef md_HelperDef = null;
        internal HelperDef HelperDef
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
            EventRegistrationTokenTable<EventHandler<object>>.GetOrCreateEventRegistrationTokenTable(ref _JumpToLastLocationEvent).InvocationList = null;
            EventRegistrationTokenTable<EventHandler<object>>.GetOrCreateEventRegistrationTokenTable(ref _AlterFontSizeEvent).InvocationList = null;
        }

        private EventRegistrationTokenTable<EventHandler<object>> _JumpToLastLocationEvent;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public event EventHandler<object> JumpToLastLocationEvent
        {
            add
            {
                DoOnMainThread(() => { wvp.JumpToLastLocationEnabled = true; });

                return EventRegistrationTokenTable<EventHandler<object>>
                    .GetOrCreateEventRegistrationTokenTable(ref _JumpToLastLocationEvent)
                    .AddEventHandler((s, e) => { Task.Run(() => value(s, e)); });
            }

            remove
            {
                DoOnMainThread(() => { wvp.JumpToLastLocationEnabled = false; });

                EventRegistrationTokenTable<EventHandler<object>>
                    .GetOrCreateEventRegistrationTokenTable(ref _JumpToLastLocationEvent)
                    .RemoveEventHandler(value);
            }
        }
        void IAo3TrackHelper.OnJumpToLastLocation(bool pagejump)
        {
            var handlers =
                EventRegistrationTokenTable<EventHandler<object>>
                .GetOrCreateEventRegistrationTokenTable(ref _JumpToLastLocationEvent)
                .InvocationList;

            //DoOnMainThread(() => {
                handlers?.Invoke(this, pagejump);
            //});
        }

        private EventRegistrationTokenTable<EventHandler<object>> _AlterFontSizeEvent;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public event EventHandler<object> AlterFontSizeEvent
        {
            add
            {
                value.Invoke(this, wvp.FontSize);

                return EventRegistrationTokenTable<EventHandler<object>>
                    .GetOrCreateEventRegistrationTokenTable(ref _AlterFontSizeEvent)
                    .AddEventHandler((s,e)=> { Task.Run(()=>value(s, e)); });
            }

            remove
            {
                EventRegistrationTokenTable<EventHandler<object>>
                    .GetOrCreateEventRegistrationTokenTable(ref _AlterFontSizeEvent)
                    .RemoveEventHandler(value);
            }
        }
        void IAo3TrackHelper.OnAlterFontSize(int fontSize)
        {
            var handlers =
                EventRegistrationTokenTable<EventHandler<object>>
                .GetOrCreateEventRegistrationTokenTable(ref _AlterFontSizeEvent)
                .InvocationList;

            //DoOnMainThread(() => {
                handlers?.Invoke(this, fontSize);
            //});
        }

        public void LogError(string name, string message, string url, int lineNo, int coloumNo, string stack)
        {
            wvp.JavascriptError(name, message, url, lineNo, coloumNo, stack);
        }

        internal static MemberDef md_GetWorkDetailsAsync = new MemberDef { @return = "FromJSON" };
        public IAsyncOperation<string> GetWorkDetailsAsync([ReadOnlyArray] long[] works, long flags)
        {
            return Task.Run(async () =>
            {
                var result = await wvp.GetWorkDetailsAsync(works, (WorkDetailsFlags)flags);
                return JsonConvert.SerializeObject(result);
            }).AsAsyncOperation();
        }

        public IAsyncOperation<string> ShouldFilterWork(long workId, [ReadOnlyArray] string[] workauthors, [ReadOnlyArray] string[] worktags, [ReadOnlyArray] long[] workserieses)
        {
            return Task.Run(async () =>
            {
                return await wvp.ShouldFilterWorkAsync(workId, workauthors, worktags, workserieses);
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
            wvp.SetWorkChaptersAsync(works);
        }

        public void ShowContextMenu(double x, double y, string url, string innerText)
        {
            wvp.DoOnMainThread(() => wvp.ShowContextMenu(x, y, url, innerText));
        }

        public void AddToReadingList(string href)
        {
            wvp.AddToReadingList(href);
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

        public bool SwipeCanGoBack
        {
            get { return DoOnMainThread(() => wvp.SwipeCanGoBack); }
        }

        public bool SwipeCanGoForward
        {
            get { return DoOnMainThread(() => wvp.SwipeCanGoForward); }
        }

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

        internal static MemberDef md_AreUrlsInReadingListAsync = new MemberDef { @return = "FromJSON" };
        public IAsyncOperation<string> AreUrlsInReadingListAsync([ReadOnlyArray] string[] urls)
        {
            return Task.Run(async () =>
            {
                var result = await wvp.AreUrlsInReadingListAsync(urls);
                return JsonConvert.SerializeObject(result);
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

        public IAsyncOperation<object> GetUnitConvOptions()
        {
            return Task.Run(() =>
            {
                return (object) wvp.UnitConvOptions;
            }).AsAsyncOperation();
        }

    }
}
