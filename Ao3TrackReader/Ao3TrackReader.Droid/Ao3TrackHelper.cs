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
    public class Ao3TrackHelper : Java.Lang.Object
    {
        WebViewPage wvp;

        public Ao3TrackHelper(WebViewPage wvp)
        {
            this.wvp = wvp;
        }

        public string ScriptsToInject
        {
            [JavascriptInterface, Export("get_scriptsToInject")]
            get { return JsonConvert.SerializeObject(wvp.ScriptsToInject); }
        }

        public string CssToInject
        {
            [JavascriptInterface, Export("get_cssToInject")]
            get { return JsonConvert.SerializeObject(wvp.CssToInject); }
        }

        public int JumpToLastLocationEvent
        {
            [JavascriptInterface, Export("get_onjumptolastlocationevent"), Converter("Event")]
            get;
            [JavascriptInterface, Export("set_onjumptolastlocationevent")]
            set;
        }
        public void OnJumpToLastLocation(bool pagejump)
        {
            Task<object>.Run(() =>
            {
                if (JumpToLastLocationEvent != 0)
                    wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", JumpToLastLocationEvent, pagejump).Wait(0);
            });
        }
        public bool JumpToLastLocationEnabled
        {
            [JavascriptInterface, Export("get_jumpToLastLocationEnabled")]
            get { return (bool)wvp.DoOnMainThread(() => wvp.JumpToLastLocationEnabled); }
            [JavascriptInterface, Export("set_jumpToLastLocationEnabled")]
            set { wvp.DoOnMainThread(() => { wvp.JumpToLastLocationEnabled = value; }); }
        }


        public int AlterFontSizeEvent
        {
            [JavascriptInterface, Export("get_onalterfontsizeevent"), Converter("Event")]
            get;
            [JavascriptInterface, Export("set_onalterfontsizeevent")]
            set;
        }
        public void OnAlterFontSize()
        {
            Task<object>.Run(() =>
            {
                if (AlterFontSizeEvent != 0)
                    wvp.CallJavascriptAsync("Ao3Track.Callbacks.CallVoid", AlterFontSizeEvent).Wait(0);
            });
        }

        public int FontSize
        {
            [JavascriptInterface, Export("get_fontSize")]
            get { return (int)wvp.DoOnMainThread(() => wvp.FontSize); }
            [JavascriptInterface, Export("set_fontSize")]
            set { wvp.DoOnMainThread(() => { wvp.FontSize = value; }); }
        }

        public void Reset()
        {
            JumpToLastLocationEvent = 0;
            AlterFontSizeEvent = 0;
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

        [JavascriptInterface, Export("showContextMenu")]
        public void ShowContextMenu(double x, double y, [Converter("ToJson")] string menuItems_json, [Converter("Callback")] int hCallback)
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
                ClipData clip = ClipData.NewPlainText("Text from Ao3",str);
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

        public string CurrentLocation {
            [JavascriptInterface, Export("get_currentLocation"), Converter("FromJSON")]
            get
            {
                var loc = wvp.DoOnMainThread(() => wvp.CurrentLocation );
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
            get {
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

    }
}

