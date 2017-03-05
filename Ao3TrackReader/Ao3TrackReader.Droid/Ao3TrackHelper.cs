﻿using System;
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
        static string s_memberDef;
        IWebViewPage wvp;

        public Ao3TrackHelper(IWebViewPage wvp)
        {
            this.wvp = wvp;
        }

        [DefIgnore]
        public string MemberDef
        {
            [JavascriptInterface, Export("get_memberDef")]
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
            onjumptolastlocationevent = 0;
            onalterfontsizeevent = 0;
        }

        int _onjumptolastlocationevent;
        public int onjumptolastlocationevent
        {
            private get
            {
                return _onjumptolastlocationevent;
            }
            [JavascriptInterface, Export("set_onjumptolastlocationevent"), Converter("Event")]
            set
            {
                _onjumptolastlocationevent = value;
                wvp.JumpToLastLocationEnabled = value != 0;
            }
        }
        void IAo3TrackHelper.OnJumpToLastLocation(bool pagejump)
        {
            Task.Run(() =>
            {
                if (onjumptolastlocationevent != 0)
                    wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", onjumptolastlocationevent, pagejump).Wait(0);
            });
        }

        int _onalterfontsizeevent;
        public int onalterfontsizeevent
        {
            private get { return _onalterfontsizeevent; }
            [JavascriptInterface, Export("set_onalterfontsizeevent"), Converter("Event")]
            set
            {
                _onalterfontsizeevent = value;
            }
        }
        void IAo3TrackHelper.OnAlterFontSize(int fontSize)
        {
            Task.Run(() =>
            {
                if (onalterfontsizeevent != 0)
                    wvp.CallJavascriptAsync("Ao3Track.Callbacks.CallVoid", onalterfontsizeevent, fontSize).Wait(0);
            });
        }


        [JavascriptInterface, Export("getWorkChaptersAsync")]
        public void GetWorkChaptersAsync([Converter("ToJSON")] string works_json, [Converter("Callback")] int hCallback)
        {
            Task.Run(async () =>
            {
                var works = JsonConvert.DeserializeObject<long[]>(works_json);
                var workchapters = await wvp.GetWorkChaptersAsync(works);
                wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", hCallback, workchapters).Wait(0);
            });
        }

        [JavascriptInterface, Export("setWorkChapters")]
        public void SetWorkChapters([Converter("ToJSON")] string workchapters_json)
        {
            Task.Run(() =>
            {
                var workchapters = JsonConvert.DeserializeObject<Dictionary<long, WorkChapter>>(workchapters_json);
                wvp.SetWorkChapters(workchapters);
            });
        }

        [JavascriptInterface, Export("hideContextMenu")]
        public void HideContextMenu()
        {
            wvp.HideContextMenu();
        }

        [JavascriptInterface, Export("showContextMenu")]
        public void ShowContextMenu(double x, double y, [Converter("ToJSON")] string menuItems_json, [Converter("Callback")] int hCallback)
        {
            Task.Run(async () =>
            {
                var menuItems = JsonConvert.DeserializeObject<string[]>(menuItems_json);
                string result = await wvp.ShowContextMenu(x, y, menuItems);
                wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", hCallback, result).Wait(0);
            });
        }

        [JavascriptInterface, Export("addToReadingList")]
        public void AddToReadingList(string href)
        {
            wvp.AddToReadingList(href);
        }
        [JavascriptInterface, Export("copyToClipboard")]
        public void CopyToClipboard(string str, string type)
        {
            var clipboard = Xamarin.Forms.Forms.Context.GetSystemService(Context.ClipboardService) as ClipboardManager;
            if (type == "text")
            {
                ClipData clip = ClipData.NewPlainText("Text from Ao3", str);
                clipboard.PrimaryClip = clip;
            }
            else if (type == "uri")
            {
                ClipData clip = ClipData.NewRawUri(str, Android.Net.Uri.Parse(str));
                clipboard.PrimaryClip = clip;
            }
        }
        [JavascriptInterface, Export("setCookies")]
        public void SetCookies(string cookies)
        {
            wvp.SetCookies(cookies);
        }


        public string NextPage
        {
            [JavascriptInterface, Export("get_nextPage")]
            get { return wvp.NextPage; }
            [JavascriptInterface, Export("set_nextPage")]
            set { wvp.DoOnMainThread(() => { wvp.NextPage = value; }); }
        }
        public string PrevPage
        {
            [JavascriptInterface, Export("get_prevPage")]
            get { return wvp.PrevPage; }
            [JavascriptInterface, Export("set_prevPage")]
            set { wvp.DoOnMainThread(() => { wvp.PrevPage = value; }); }
        }

        public bool CanGoBack
        {
            [JavascriptInterface, Export("get_canGoBack")]
            get { return (bool)wvp.DoOnMainThread(() => wvp.CanGoBack); }
        }
        public bool CanGoForward
        {
            [JavascriptInterface, Export("get_canGoForward")]
            get { return (bool)wvp.DoOnMainThread(() => wvp.CanGoForward); }
        }

        [JavascriptInterface, Export("goBack")]
        public void GoBack() { wvp.DoOnMainThread(() => wvp.GoBack()); }

        [JavascriptInterface, Export("goForward")]
        public void GoForward() { wvp.DoOnMainThread(() => wvp.GoForward()); }

        public double LeftOffset
        {
            [JavascriptInterface, Export("get_leftOffset")]
            get { return (double)wvp.DoOnMainThread(() => wvp.LeftOffset); }
            [JavascriptInterface, Export("set_leftOffset")]
            set { wvp.DoOnMainThread(() => { wvp.LeftOffset = value; }); }
        }
        public int ShowPrevPageIndicator
        {
            [JavascriptInterface, Export("get_showPrevPageIndicator")]
            get { return (int)wvp.DoOnMainThread(() => wvp.ShowPrevPageIndicator); }
            [JavascriptInterface, Export("set_showPrevPageIndicator")]
            set { wvp.DoOnMainThread(() => { wvp.ShowPrevPageIndicator = value; }); }
        }
        public int ShowNextPageIndicator
        {
            [JavascriptInterface, Export("get_showNextPageIndicator")]
            get { return (int)wvp.DoOnMainThread(() => wvp.ShowNextPageIndicator); }
            [JavascriptInterface, Export("set_showNextPageIndicator")]
            set { wvp.DoOnMainThread(() => { wvp.ShowNextPageIndicator = value; }); }
        }

        public string CurrentLocation
        {
            [JavascriptInterface, Export("get_currentLocation"), Converter("FromJSON")]
            get
            {
                var loc = wvp.DoOnMainThread(() => wvp.CurrentLocation);
                if (loc == null) return null;
                return JsonConvert.SerializeObject(loc);
            }
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
            [JavascriptInterface, Export("get_pageTitle"), Converter("FromJSON")]
            get
            {
                var pagetitle = wvp.DoOnMainThread(() => wvp.PageTitle);
                if (pagetitle == null) return null;
                return JsonConvert.SerializeObject(pagetitle);
            }
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
        public void AreUrlsInReadingListAsync([Converter("ToJSON")] string urls_json, [Converter("Callback")] int hCallback)
        {

            Task.Run(async () =>
            {
                var urls = JsonConvert.DeserializeObject<string[]>(urls_json);
                var res = await wvp.AreUrlsInReadingListAsync(urls);
                wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", hCallback, res).Wait(0);
            });
        }

        [JavascriptInterface, Export("startWebViewDragAccelerate")]
        public void StartWebViewDragAccelerate(double velocity)
        {
            wvp.DoOnMainThread(() => wvp.StartWebViewDragAccelerate(velocity));
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
    }
}

