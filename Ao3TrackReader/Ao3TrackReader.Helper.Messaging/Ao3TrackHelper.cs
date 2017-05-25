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
                wvp.DoOnMainThread(() => { wvp.JumpToLastLocationEnabled = value != 0; });
            }
        }
        void IAo3TrackHelper.OnJumpToLastLocation(bool pagejump)
        {
            wvp.DoOnMainThread(() =>
            {
                if (_onjumptolastlocationevent != 0)
                    wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", _onjumptolastlocationevent, pagejump);
            });
        }


        int _onalterfontsizeevent;
        public int onalterfontsizeevent
        {
            private get { return _onalterfontsizeevent; }
            [Converter("Event")]
            set
            {
                if (value != 0) wvp.DoOnMainThread(async () => await wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", value, wvp.FontSize));
                _onalterfontsizeevent = value;
            }
        }

        void IAo3TrackHelper.OnAlterFontSize(int fontSize)
        {
            if (_onalterfontsizeevent != 0)
                wvp.DoOnMainThread(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", _onalterfontsizeevent, fontSize); });
        }

        public void LogError(string name, string message, string url, int lineNo, int coloumNo, string stack)
        {
            wvp.JavascriptError(name, message, url, lineNo, coloumNo, stack);
        }

        public async void GetWorkDetailsAsync(long[] works, int flags, [Converter("Callback")] int hCallback)
        {
            var workchapters = await wvp.GetWorkDetailsAsync(works, (WorkDetailsFlags)flags);
            wvp.DoOnMainThread(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", hCallback, workchapters); });
        }

        public void SetWorkChapters(Dictionary<long, WorkChapter> workchapters)
        {
            wvp.SetWorkChaptersAsync(workchapters);
        }

        public async void ShouldFilterWork(long workId, string[] workauthors, string[] worktags, long[] workserieses, [Converter("Callback")] int hCallback)
        {
            var result = await wvp.ShouldFilterWorkAsync(workId, workauthors, worktags, workserieses);
            wvp.DoOnMainThread(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", hCallback, result); });
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
            set { wvp.DoOnMainThread(() => {
                wvp.NextPage = value;
                wvp.CallJavascriptAsync("Ao3Track.Messaging.helper.setValue", "swipeCanGoForward", wvp.SwipeCanGoForward);
            }); }

        }
        public string PrevPage
        {
            set { wvp.DoOnMainThread(() => {
                wvp.PrevPage = value;
                wvp.CallJavascriptAsync("Ao3Track.Messaging.helper.setValue", "swipeCanGoBack", wvp.SwipeCanGoBack);
            }); }
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
            set { wvp.DoOnMainThread(() => { wvp.LeftOffset = value; }); }
        }
        public int ShowPrevPageIndicator
        {
            set { wvp.DoOnMainThread(() => { wvp.ShowPrevPageIndicator = value; }); }
        }
        public int ShowNextPageIndicator
        {
            set { wvp.DoOnMainThread(() => { wvp.ShowNextPageIndicator = value; }); }
        }

        public WorkChapter CurrentLocation
        {
            set
            {
                wvp.DoOnMainThread(() =>
                {
                    wvp.CurrentLocation = value;
                });
            }
        }

        public PageTitle PageTitle
        {
            set
            {
                wvp.DoOnMainThread(() =>
                {
                    wvp.PageTitle = value;
                });
            }
        }

        public async void AreUrlsInReadingListAsync(string[] urls, [Converter("Callback")] int hCallback)
        {
            var res = await wvp.AreUrlsInReadingListAsync(urls);
            wvp.DoOnMainThread(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", hCallback, res); });
        }

        public void StartWebViewDragAccelerate(double velocity)
        {
            wvp.DoOnMainThread(() => { wvp.StartWebViewDragAccelerate(velocity); });
        }

        public void StopWebViewDragAccelerate()
        {
            wvp.DoOnMainThread(() => { wvp.StopWebViewDragAccelerate(); });
        }

        public double DeviceWidth
        {
            get
            {
                throw new NotSupportedException();
            }
        }
    }
}
