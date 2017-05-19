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
using Android.Webkit;
using Android.Content;
using Ao3TrackReader.Helper;
using Java.Interop;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Ao3TrackReader.Helper
{
    public class Ao3TrackHelper : Java.Lang.Object, IAo3TrackHelper
    {
        IWebViewPage wvp;

        public Ao3TrackHelper(IWebViewPage wvp)
        {
            this.wvp = wvp;
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Kitkat)
            {
                evals = new Dictionary<int, Tuple<string, TaskCompletionSource<string>>>();
            }
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
        [DefIgnore]
        public string HelperDefJson
        {
            [JavascriptInterface, Export("get_helperDefJson")]
            get
            {
                if (s_helperDefJson == null) s_helperDefJson = HelperDef.Serialize();
                return s_helperDefJson;
            }
        }

        // Old version of android is really annoying. Doesn't have WebView.EvaluateJavascript method
        // but navigating to a javascript: url will work
        // So using the injected object and a javascript url, we can effectively emulate the entire functionality of the missing method

        int evalindex = 0;
        Dictionary<int, Tuple<string, TaskCompletionSource<string>>> evals;

        string IAo3TrackHelper.GetEvalJavascriptUrl(string code, TaskCompletionSource<string> cs)
        {
            if (evals == null) throw new PlatformNotSupportedException("This function is intended for Android API 18 and earlier");

            var def = new Tuple<string, TaskCompletionSource<string>>(code, cs);
            evals.Add(++evalindex, def);

            if (cs != null) return string.Format("javascript:Ao3TrackHelperNative.setEvalResult({0}, eval(Ao3TrackHelperNative.getEvalCode({0})))", evalindex);
            else return string.Format("javascript:eval(Ao3TrackHelperNative.getEvalCode({0}))", evalindex);
        }

        [DefIgnore, JavascriptInterface, Export("getEvalCode")]
        public string GetEvalCode(int index)
        {
            if (evals.TryGetValue(index, out var def))
            {
                if (def.Item2 == null) evals.Remove(index);
                return def.Item1;
            }
            return "";
        }

        [DefIgnore, JavascriptInterface, Export("setEvalResult")]
        public void SetEvalResult(int index, string result)
        {
            if (evals.TryGetValue(index, out var def))
            {
                if (def.Item2 != null) def.Item2.TrySetResult(result);
                evals.Remove(index);               
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
            [JavascriptInterface, Export("set_onjumptolastlocationevent"), Converter("Event")]
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
            [JavascriptInterface, Export("set_onalterfontsizeevent"), Converter("Event")]
            set
            {
                if (value != 0) wvp.DoOnMainThread(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", value, wvp.FontSize); });
                _onalterfontsizeevent = value;
            }
        }
        void IAo3TrackHelper.OnAlterFontSize(int fontSize)
        {
            if (_onalterfontsizeevent != 0)
                wvp.DoOnMainThread(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", _onalterfontsizeevent, fontSize); });
        }

        [JavascriptInterface, Export("logError")]
        public void LogError(string name, string message, string url, int lineNo, int coloumNo, string stack)
        {
            wvp.JavascriptError(name, message, url, lineNo, coloumNo, stack);
        }

        [JavascriptInterface, Export("getWorkDetailsAsync")]
        public async void GetWorkDetailsAsync([Converter("ToJSON")] string works_json, int flags, [Converter("Callback")] int hCallback)
        {
            var works = JsonConvert.DeserializeObject<long[]>(works_json);
            var workchapters = await wvp.GetWorkDetailsAsync(works,(WorkDetailsFlags) flags);
            wvp.DoOnMainThread(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", hCallback, workchapters); });
        }

        [JavascriptInterface, Export("setWorkChapters")]
        public void SetWorkChapters([Converter("ToJSON")] string workchapters_json)
        {
            var workchapters = JsonConvert.DeserializeObject<Dictionary<long, WorkChapter>>(workchapters_json);
            wvp.SetWorkChaptersAsync(workchapters);
        }

        [JavascriptInterface, Export("showContextMenu")]
        public void ShowContextMenu(double x, double y, string url, string innerHtml)
        {
            wvp.DoOnMainThread(() => { wvp.ShowContextMenu(x, y, url, innerHtml); } );
        }

        [JavascriptInterface, Export("addToReadingList")]
        public void AddToReadingList(string href)
        {
            wvp.AddToReadingList(href);
        }

        [JavascriptInterface, Export("setCookies")]
        public void SetCookies(string cookies)
        {
            wvp.SetCookies(cookies);
        }

        public string NextPage
        {
            [JavascriptInterface, Export("set_nextPage")]
            set { wvp.DoOnMainThread(() => { wvp.NextPage = value; }); }
        }
        public string PrevPage
        {
            [JavascriptInterface, Export("set_prevPage")]
            set { wvp.DoOnMainThread(() => { wvp.PrevPage = value; }); }
        }

        public bool SwipeCanGoBack
        {
            [JavascriptInterface, Export("get_swipeCanGoBack")]
            get { return wvp.DoOnMainThread(() => wvp.SwipeCanGoBack); }
        }
        public bool SwipeCanGoForward
        {
            [JavascriptInterface, Export("get_swipeCanGoForward")]
            get { return wvp.DoOnMainThread(() => wvp.SwipeCanGoForward); }
        }

        public double LeftOffset
        {
            [JavascriptInterface, Export("get_leftOffset")]
            get { return wvp.DoOnMainThread(() => wvp.LeftOffset); }
            [JavascriptInterface, Export("set_leftOffset")]
            set { wvp.DoOnMainThread(() => { wvp.LeftOffset = value; }); }
        }
        public int ShowPrevPageIndicator
        {
            [JavascriptInterface, Export("set_showPrevPageIndicator")]
            set { wvp.DoOnMainThread(() => { wvp.ShowPrevPageIndicator = value; }); }
        }
        public int ShowNextPageIndicator
        {
            [JavascriptInterface, Export("set_showNextPageIndicator")]
            set { wvp.DoOnMainThread(() => { wvp.ShowNextPageIndicator = value; }); }
        }

        public string CurrentLocation
        {
            [JavascriptInterface, Export("set_currentLocation"), Converter("ToJSON")]
            set
            {
                wvp.DoOnMainThread(() =>
                {
                    if (value == null || value == "(null)" || value == "null") wvp.CurrentLocation = null;
                    else wvp.CurrentLocation = JsonConvert.DeserializeObject<WorkChapter>(value);
                });
            }
        }

        public string PageTitle
        {
            [JavascriptInterface, Export("set_pageTitle"), Converter("ToJSON")]
            set
            {
                wvp.DoOnMainThread(() =>
                {
                    if (value == null || value == "(null)" || value == "null") wvp.PageTitle = null;
                    else wvp.PageTitle = JsonConvert.DeserializeObject<PageTitle>(value);
                });
            }
        }

        [JavascriptInterface, Export("areUrlsInReadingListAsync")]
        public async void AreUrlsInReadingListAsync([Converter("ToJSON")] string urls_json, [Converter("Callback")] int hCallback)
        {
            var urls = JsonConvert.DeserializeObject<string[]>(urls_json);
            var res = await wvp.AreUrlsInReadingListAsync(urls);
            wvp.DoOnMainThread(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", hCallback, res); });
        }

        [JavascriptInterface, Export("startWebViewDragAccelerate")]
        public void StartWebViewDragAccelerate(double velocity)
        {
            wvp.DoOnMainThread(() => { wvp.StartWebViewDragAccelerate(velocity); });
        }

        [JavascriptInterface, Export("stopWebViewDragAccelerate")]
        public void StopWebViewDragAccelerate()
        {
            wvp.StopWebViewDragAccelerate();
        }

        public double DeviceWidth
        {
            [JavascriptInterface, Export("get_deviceWidth")]
            get
            {
                return wvp.DeviceWidth;
            }
        }

        [JavascriptInterface, Export("getUnitConvOptions")]
        public void GetUnitConvOptions([Converter("Callback")] int hCallback)
        {
            wvp.DoOnMainThread(() => { wvp.CallJavascriptAsync("Ao3Track.Callbacks.call", hCallback, wvp.UnitConvOptions); });
        }

    }
}

