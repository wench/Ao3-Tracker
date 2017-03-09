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
using WebKit;

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
        public HelperDef HelperDef
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
                    wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", _onjumptolastlocationevent, pagejump).Wait(0);
            });
        }


        int _onalterfontsizeevent;
        public int onalterfontsizeevent
        {
            private get { return _onalterfontsizeevent; }
            [Converter("Event")]
            set
            {
                if (value != 0) wvp.DoOnMainThread(async () => await wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", value, wvp.FontSize));
                _onalterfontsizeevent = value;
            }
        }

        void IAo3TrackHelper.OnAlterFontSize(int fontSize)
        {
            if (_onalterfontsizeevent != 0)
                wvp.DoOnMainThread(async () => await wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", _onalterfontsizeevent, fontSize));
        }


        public void GetWorkChaptersAsync(long[] works, [Converter("Callback")] int hCallback)
        {
            Task.Run(async () =>
            {
                var workchapters = await wvp.GetWorkChaptersAsync(works);
                wvp.DoOnMainThread(() => wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", hCallback, workchapters).Wait(0));
            });
        }

        public void SetWorkChapters(Dictionary<long, WorkChapter> workchapters)
        {
            Task.Run(() =>
            {
                wvp.SetWorkChapters(workchapters);
            });
        }

        public void ShowContextMenu(double x, double y, string url, string innerHtml)
        {
            wvp.ShowContextMenu(x, y, url, innerHtml);
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
            set { wvp.DoOnMainThread(async () => {
                wvp.NextPage = value;
                await wvp.CallJavascriptAsync("Ao3Track.iOS.helper.setValue", "swipeCanGoForward", wvp.SwipeCanGoForward);
            }); }

        }
        public string PrevPage
        {
            set { wvp.DoOnMainThread(async () => {
                wvp.PrevPage = value;
                await wvp.CallJavascriptAsync("Ao3Track.iOS.helper.setValue", "swipeCanGoBack", wvp.SwipeCanGoBack);
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

        public void AreUrlsInReadingListAsync(string[] urls, [Converter("Callback")] int hCallback)
        {
            Task.Run(async () =>
            {
                var res = await wvp.AreUrlsInReadingListAsync(urls);
                await wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", hCallback, res);
            });
        }

        public void StartWebViewDragAccelerate(double velocity)
        {
            wvp.DoOnMainThread(() => wvp.StartWebViewDragAccelerate(velocity));
        }

        public void StopWebViewDragAccelerate()
        {
            wvp.StopWebViewDragAccelerate();
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
