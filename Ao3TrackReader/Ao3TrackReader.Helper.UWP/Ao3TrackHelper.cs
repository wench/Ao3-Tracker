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

        private async Task<T> DoOnMainThreadAsync<T>(Func<T> func)
        {
            return (T)await wvp.DoOnMainThreadAsync(() =>
                func()
            );
        }

        private async Task<T> DoOnMainThreadAsync<T>(Func<Task<T>> func)
        {
            return (T)await wvp.DoOnMainThreadAsync(() =>
                Task.Run(async () =>
                    (object)await func()
                ).AsAsyncOperation()
            );
        }

        private async Task DoOnMainThreadAsync(Action func)
        {
            await wvp.DoOnMainThreadAsync(() =>
                func()
            );
        }

        private async Task DoOnMainThreadAsync(Func<Task> func)
        {
            await wvp.DoOnMainThreadAsync(() =>
                Task.Run(async () =>
                    await func()
                ).AsAsyncAction()
            );
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
            EventRegistrationTokenTable<EventHandler<object>>.GetOrCreateEventRegistrationTokenTable(ref _RequestSpeechText).InvocationList = null;
        }

        private EventRegistrationTokenTable<EventHandler<object>> _JumpToLastLocationEvent;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public event EventHandler<object> JumpToLastLocationEvent
        {
            add
            {
                DoOnMainThreadAsync(() => { wvp.JumpToLastLocationEnabled = true; }).Wait();

                return EventRegistrationTokenTable<EventHandler<object>>
                    .GetOrCreateEventRegistrationTokenTable(ref _JumpToLastLocationEvent)
                    .AddEventHandler((s, e) => { Task.Run(() => value(s, e)); });
            }

            remove
            {
                DoOnMainThreadAsync(() => { wvp.JumpToLastLocationEnabled = false; }).Wait();

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

            handlers?.Invoke(this, pagejump);
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
                    .AddEventHandler((s, e) => { Task.Run(() => value(s, e)); });
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

            handlers?.Invoke(this, fontSize);
        }

        private EventRegistrationTokenTable<EventHandler<object>> _RequestSpeechText;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public event EventHandler<object> RequestSpeechText
        {
            add
            {
                var ret = EventRegistrationTokenTable<EventHandler<object>>
                    .GetOrCreateEventRegistrationTokenTable(ref _RequestSpeechText)
                    .AddEventHandler((s, e) => { Task.Run(() => value(s, e)); });

                DoOnMainThreadAsync(() => { wvp.HasSpeechText = true; }).Wait();

                return ret;
            }

            remove
            {
                DoOnMainThreadAsync(() => { wvp.HasSpeechText = false; }).Wait();

                EventRegistrationTokenTable<EventHandler<object>>
                    .GetOrCreateEventRegistrationTokenTable(ref _RequestSpeechText)
                    .RemoveEventHandler(value);
            }
        }
        void IAo3TrackHelper.OnRequestSpeechText()
        {
            var handlers =
                EventRegistrationTokenTable<EventHandler<object>>
                .GetOrCreateEventRegistrationTokenTable(ref _RequestSpeechText)
                .InvocationList;

            handlers?.Invoke(this, true);
        }

        public void Init()
        {
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

                case "SpeechTextNative":
                    return new SpeechText();

                case "SpeechTextChapterNative":
                    return new SpeechTextChapter();
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
            DoOnMainThreadAsync(() => wvp.ShowContextMenu(x, y, url, innerText)).ConfigureAwait(false);
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
            set { DoOnMainThreadAsync(() => { wvp.NextPage = value; }).ConfigureAwait(false); }
        }

        internal static MemberDef md_PrevPage = new MemberDef { getter = "false" };
        public string PrevPage
        {
            get { throw new NotSupportedException(); }
            set { DoOnMainThreadAsync(() => { wvp.PrevPage = value; }).ConfigureAwait(false); }
        }

        public bool SwipeCanGoBack
        {
            get { return DoOnMainThreadAsync(() => wvp.SwipeCanGoBack).Result; }
        }

        public bool SwipeCanGoForward
        {
            get { return DoOnMainThreadAsync(() => wvp.SwipeCanGoForward).Result; }
        }

        public double LeftOffset
        {
            get { return DoOnMainThreadAsync(() => wvp.LeftOffset).Result; }
            set { DoOnMainThreadAsync(() => { wvp.LeftOffset = value; }).ConfigureAwait(false); }
        }

        internal static MemberDef md_ShowPrevPageIndicator = new MemberDef { getter = "false" };
        public int ShowPrevPageIndicator
        {
            get { throw new NotSupportedException(); }
            set { DoOnMainThreadAsync(() => { wvp.ShowPrevPageIndicator = value; }).ConfigureAwait(false); }
        }

        internal static MemberDef md_ShowNextPageIndicator = new MemberDef { getter = "false" };
        public int ShowNextPageIndicator
        {
            get { throw new NotSupportedException(); }
            set { DoOnMainThreadAsync(() => { wvp.ShowNextPageIndicator = value; }).ConfigureAwait(false); }
        }

        internal static MemberDef md_CurrentLocation = new MemberDef { getter = "false", setter = "ToWorkChapterExNative" };
        public IWorkChapterEx CurrentLocation
        {
            get { throw new NotSupportedException(); }
            set { DoOnMainThreadAsync(() => { wvp.CurrentLocation = value; }).ConfigureAwait(false); }
        }

        internal static MemberDef md_PageTitle = new MemberDef { getter = "false", setter = "ToPageTitleNative" };
        public PageTitle PageTitle
        {
            get { throw new NotSupportedException(); }
            set { DoOnMainThreadAsync(() => { wvp.PageTitle = value; }).ConfigureAwait(false); }
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
            DoOnMainThreadAsync(() => wvp.StartWebViewDragAccelerate(velocity)).ConfigureAwait(false);
        }

        public void StopWebViewDragAccelerate()
        {
            wvp.StopWebViewDragAccelerate();
        }

        public double DeviceWidth
        {
            get
            {
                return DoOnMainThreadAsync(() => wvp.DeviceWidth).Result;
            }
        }

        public ISettings Settings
        {
            get => wvp.Settings;
        }

        internal static MemberDef md_SetSpeechText = new MemberDef { args = new Dictionary<int, string> { { 0, "ToSpeechTextNative" } } };
        public void SetSpeechText(SpeechText speechText)
        {
            DoOnMainThreadAsync(() => wvp.SetSpeechText(speechText)).ConfigureAwait(false);
        }
    }
}
