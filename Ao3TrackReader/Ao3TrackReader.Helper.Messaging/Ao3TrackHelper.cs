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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ao3TrackReader.Helper
{
    public class Ao3TrackHelper : IAo3TrackHelper
    {
        IWebViewPage wvp;

        public Ao3TrackHelper(IWebViewPage wvp)
        {
            this.wvp = wvp;
        }

        static HelperDef s_def;
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
        [DefIgnore]
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
            _onjumptolastlocationevent = 0;
            _onalterfontsizeevent = 0;
            _onrequestspeechtext = 0;
        }

        public void Init()
        {
            wvp.DoOnMainThreadAsync(async () =>
            {
                await wvp.EvaluateJavascriptAsync(string.Format(
                    "Ao3Track.Messaging.helper.setValue({0},{1});" +
                    "Ao3Track.Messaging.helper.setValue({2},{3});" +
                    "Ao3Track.Messaging.helper.setValue({4},{5});" +
                    "Ao3Track.Messaging.helper.setValue({6},{7});" +
                    "Ao3Track.Messaging.helper.setValue({8},{9});",
                    JsonConvert.SerializeObject("leftOffset"), JsonConvert.SerializeObject(wvp.LeftOffset),
                    JsonConvert.SerializeObject("swipeCanGoBack"), JsonConvert.SerializeObject(wvp.SwipeCanGoBack),
                    JsonConvert.SerializeObject("swipeCanGoForward"), JsonConvert.SerializeObject(wvp.SwipeCanGoForward),
                    JsonConvert.SerializeObject("deviceWidth"), JsonConvert.SerializeObject(wvp.DeviceWidth),
                    JsonConvert.SerializeObject("settings"), JsonConvert.SerializeObject(wvp.Settings))
                    );
            }).ConfigureAwait(false);
        }

        int _onjumptolastlocationevent;
        public int onjumptolastlocationevent
        {
            private get { return _onjumptolastlocationevent; }
            [Converter("Event")]
            set
            {
                _onjumptolastlocationevent = value;
                wvp.DoOnMainThreadAsync(() => { wvp.JumpToLastLocationEnabled = value != 0; }).ConfigureAwait(false);
            }
        }
        void IAo3TrackHelper.OnJumpToLastLocation(bool pagejump)
        {
            wvp.DoOnMainThreadAsync(() =>
            {
                if (_onjumptolastlocationevent != 0)
                    wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", _onjumptolastlocationevent, pagejump);
            }).ConfigureAwait(false);
        }


        int _onalterfontsizeevent;
        public int onalterfontsizeevent
        {
            private get { return _onalterfontsizeevent; }
            [Converter("Event")]
            set
            {
                if (value != 0) wvp.DoOnMainThreadAsync(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", value, wvp.FontSize); }).ConfigureAwait(false);
                _onalterfontsizeevent = value;
            }
        }

        void IAo3TrackHelper.OnAlterFontSize(int fontSize)
        {
            if (_onalterfontsizeevent != 0)
                wvp.DoOnMainThreadAsync(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", _onalterfontsizeevent, fontSize); }).ConfigureAwait(false);
        }

        int _onrequestspeechtext;
        public int onrequestspeechtext
        {
            [Converter("Event")]
            set
            {
                _onrequestspeechtext = value;
                wvp.DoOnMainThreadAsync(() => { wvp.HasSpeechText = value != 0; }).ConfigureAwait(false);
            }
        }
        void IAo3TrackHelper.OnRequestSpeechText()
        {
            if (_onrequestspeechtext != 0)
                wvp.DoOnMainThreadAsync(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", _onrequestspeechtext, true); }).ConfigureAwait(false);
        }

        public void LogError(string name, string message, string url, string file, int lineNo, int coloumNo, string stack)
        {
            wvp.JavascriptError(name, message, url, file, lineNo, coloumNo, stack);
        }

        public async void GetWorkDetailsAsync(long[] works, int flags, [Converter("Callback")] int hCallback)
        {
            var workchapters = await wvp.GetWorkDetailsAsync(works, (WorkDetailsFlags)flags);
            await wvp.DoOnMainThreadAsync(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", hCallback, workchapters); }).ConfigureAwait(false);
        }

        public void SetWorkChapters(Dictionary<long, WorkChapter> workchapters)
        {
            wvp.SetWorkChaptersAsync(workchapters);
        }

        public async void ShouldFilterWork(long workId, string[] workauthors, string[] worktags, long[] workserieses, [Converter("Callback")] int hCallback)
        {
            var result = await wvp.ShouldFilterWorkAsync(workId, workauthors, worktags, workserieses);
            await wvp.DoOnMainThreadAsync(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", hCallback, result); }).ConfigureAwait(false);
        }

        public void ShowContextMenu(double x, double y, string url, string innerText    )
        {
            wvp.ShowContextMenu(x, y, url, innerText);
        }

        public void AddToReadingList(string href)
        {
            wvp.AddToReadingList(href);
        }

        public void SetCookies(string cookies)
        {
            wvp.SetCookies(cookies);
        }

        public string NextPage
        {
            set { wvp.DoOnMainThreadAsync(() => {
                wvp.NextPage = value;
                wvp.CallJavascriptAsync("Ao3Track.Messaging.helper.setValue", "swipeCanGoForward", wvp.SwipeCanGoForward);
            }).ConfigureAwait(false); }

        }
        public string PrevPage
        {
            set { wvp.DoOnMainThreadAsync(() => {
                wvp.PrevPage = value;
                wvp.CallJavascriptAsync("Ao3Track.Messaging.helper.setValue", "swipeCanGoBack", wvp.SwipeCanGoBack);
            }).ConfigureAwait(false); }
        }

        public bool SwipeCanGoBack
        {
            get { throw new NotSupportedException(); }
        }
        public bool SwipeCanGoForward
        {
            get { throw new NotSupportedException(); }
        }

        public double LeftOffset
        {
            get { throw new NotSupportedException(); }
            set { wvp.DoOnMainThreadAsync(() => { wvp.LeftOffset = value; }).ConfigureAwait(false); }
        }
        public int ShowPrevPageIndicator
        {
            set { wvp.DoOnMainThreadAsync(() => { wvp.ShowPrevPageIndicator = value; }).ConfigureAwait(false); }
        }
        public int ShowNextPageIndicator
        {
            set { wvp.DoOnMainThreadAsync(() => { wvp.ShowNextPageIndicator = value; }).ConfigureAwait(false); }
        }

        public WorkChapter CurrentLocation
        {
            set
            {
                wvp.DoOnMainThreadAsync(() =>
                {
                    wvp.CurrentLocation = value;
                }).ConfigureAwait(false);
            }
        }

        public PageTitle PageTitle
        {
            set
            {
                wvp.DoOnMainThreadAsync(() =>
                {
                    wvp.PageTitle = value;
                }).ConfigureAwait(false);
            }
        }

        public async void AreUrlsInReadingListAsync(string[] urls, [Converter("Callback")] int hCallback)
        {
            var res = await wvp.AreUrlsInReadingListAsync(urls);
            await wvp.DoOnMainThreadAsync(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", hCallback, res); }).ConfigureAwait(false);
        }

        public void StartWebViewDragAccelerate(double velocity)
        {
            wvp.DoOnMainThreadAsync(() => { wvp.StartWebViewDragAccelerate(velocity); }).ConfigureAwait(false);
        }

        public void StopWebViewDragAccelerate()
        {
            wvp.DoOnMainThreadAsync(() => { wvp.StopWebViewDragAccelerate(); }).ConfigureAwait(false);
        }

        public double DeviceWidth
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public ISettings Settings
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public void SetSpeechText(SpeechText speechText)
        {
            wvp.DoOnMainThreadAsync(() => wvp.SetSpeechText(speechText)).ConfigureAwait(false);
        }
    }
}
