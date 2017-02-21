using System;
using Android.Webkit;
using Android.Content;
using Ao3TrackReader.Helper;
using Java.Interop;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Ao3TrackReader.Droid
{
    public class Ao3TrackHelper : Java.Lang.Object
    {
        WebViewPage handler;

        public Ao3TrackHelper(WebViewPage handler)
        {
            this.handler = handler;
        }

        public string ScriptsToInject
        {
            [JavascriptInterface, Export("get_ScriptsToInject")]
            get { return JsonConvert.SerializeObject(handler.ScriptsToInject); }
        }

        public string CssToInject
        {
            [JavascriptInterface, Export("get_CssToInject")]
            get { return JsonConvert.SerializeObject(handler.CssToInject); }
        }

        public int JumpToLastLocationEvent
        {
            [JavascriptInterface, Export("get_onjumptolastlocationevent")]
            get;
            [JavascriptInterface, Export("set_onjumptolastlocationevent")]
            set;
        }
        public void OnJumpToLastLocation(bool pagejump)
        {
            Task<object>.Run(() =>
            {
                if (JumpToLastLocationEvent != 0)
                    handler.CallJavascript("Ao3TrackCallbacks.Call",JumpToLastLocationEvent, pagejump);
            });
        }
        public bool JumpToLastLocationEnabled
        {
            [JavascriptInterface, Export("get_JumpToLastLocationEnabled")]
            get { return (bool)handler.DoOnMainThread(() => handler.JumpToLastLocationEnabled); }
            [JavascriptInterface, Export("set_JumpToLastLocationEnabled")]
            set { handler.DoOnMainThread(() => { handler.JumpToLastLocationEnabled = value; }); }
        }


        public int AlterFontSizeEvent
        {
            [JavascriptInterface, Export("get_onalterfontsizeevent")]
            get;
            [JavascriptInterface, Export("set_onalterfontsizeevent")]
            set;
        }
        public void OnAlterFontSize()
        {
            Task<object>.Run(() =>
            {
                if (AlterFontSizeEvent != 0)
                    handler.CallJavascript("Ao3TrackCallbacks.CallVoid", AlterFontSizeEvent);
            });
        }

        public int FontSize
        {
            [JavascriptInterface, Export("get_FontSize")]
            get { return (int)handler.DoOnMainThread(() => handler.FontSize); }
            [JavascriptInterface, Export("set_FontSize")]
            set { handler.DoOnMainThread(() => { handler.FontSize = value; }); }
        }

        public void Reset()
        {
            JumpToLastLocationEvent = 0;
            AlterFontSizeEvent = 0;
        }


        [JavascriptInterface, Export("getWorkChaptersAsync")]
        public void GetWorkChaptersAsync(string works_json, int hCallback)
        {
            Task.Run(async () =>
            {
                var works = JsonConvert.DeserializeObject<long[]>(works_json);
                var workchapters = await handler.GetWorkChaptersAsync(works);
                handler.CallJavascript("Ao3TrackCallbacks.Call", hCallback, JsonConvert.SerializeObject(workchapters));
            });
        }

        [JavascriptInterface, Export("setWorkChapters")]
        public void SetWorkChapters(string workchapters_json)
        {
            Task.Run(() =>
            {
                var workchapters = JsonConvert.DeserializeObject<Dictionary<long, WorkChapter>>(workchapters_json);
                handler.SetWorkChapters(workchapters);
            });
        }

        [JavascriptInterface, Export("showContextMenu")]
        public void ShowContextMenu(double x, double y, string menuItems_json, int hCallback)
        {
            Task.Run(async () =>
            {
                var menuItems = JsonConvert.DeserializeObject<string[]>(menuItems_json);
                string result = await handler.ShowContextMenu(x, y, menuItems);
                handler.CallJavascript("Ao3TrackCallbacks.Call", hCallback, result);
            });
        }

        [JavascriptInterface, Export("addToReadingList")]
        public void AddToReadingList(string href)
        {
            handler.AddToReadingList(href);
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
            handler.SetCookies(cookies);
        }


        public string NextPage
        {
            [JavascriptInterface, Export("get_NextPage")]
            get { return handler.NextPage; }
            [JavascriptInterface, Export("set_NextPage")]
            set { handler.DoOnMainThread(() => { handler.NextPage = value; }); }
        }
        public string PrevPage
        {
            [JavascriptInterface, Export("get_PrevPage")]
            get { return handler.PrevPage; }
            [JavascriptInterface, Export("set_PrevPage")]
            set { handler.DoOnMainThread(() => { handler.PrevPage = value; }); }
        }

        public bool CanGoBack
        {
            [JavascriptInterface, Export("get_CanGoBack")]
            get { return (bool)handler.DoOnMainThread(() => handler.CanGoBack); }
        }
        public bool CanGoForward
        {
            [JavascriptInterface, Export("get_CanGoForward")]
            get { return (bool)handler.DoOnMainThread(() => handler.CanGoForward); }
        }

        [JavascriptInterface, Export("goBack")]
        public void GoBack() { handler.DoOnMainThread(() => handler.GoBack()); }

        [JavascriptInterface, Export("goForward")]
        public void GoForward() { handler.DoOnMainThread(() => handler.GoForward()); }

        public double LeftOffset
        {
            [JavascriptInterface, Export("get_LeftOffset")]
            get { return (double)handler.DoOnMainThread(() => handler.LeftOffset); }
            [JavascriptInterface, Export("set_LeftOffset")]
            set { handler.DoOnMainThread(() => { handler.LeftOffset = value; }); }
        }
        public bool ShowPrevPageIndicator
        {
            [JavascriptInterface, Export("get_ShowPrevPageIndicator")]
            get { return (bool)handler.DoOnMainThread(() => handler.ShowPrevPageIndicator); }
            [JavascriptInterface, Export("set_ShowPrevPageIndicator")]
            set { handler.DoOnMainThread(() => { handler.ShowPrevPageIndicator = value; }); }
        }
        public bool ShowNextPageIndicator
        {
            [JavascriptInterface, Export("get_ShowNextPageIndicator")]
            get { return (bool)handler.DoOnMainThread(() => handler.ShowNextPageIndicator); }
            [JavascriptInterface, Export("set_ShowNextPageIndicator")]
            set { handler.DoOnMainThread(() => { handler.ShowNextPageIndicator = value; }); }
        }

        public string CurrentLocation {
            [JavascriptInterface, Export("get_CurrentLocation")]
            get
            {
                var loc = handler.DoOnMainThread(() => handler.CurrentLocation );
                if (loc == null) return null;
                return JsonConvert.SerializeObject(loc);
            }
            [JavascriptInterface, Export("set_CurrentLocation")]
            set
            {
                handler.DoOnMainThread(() =>
                {
                    if (value == null || value == "(null)" || value == "null") handler.CurrentLocation = null;
                    else handler.CurrentLocation = JsonConvert.DeserializeObject<WorkChapter>(value);
                });
            }
        }

        public string PageTitle
        {
            [JavascriptInterface, Export("get_PageTitle")]
            get {
                var pagetitle = handler.DoOnMainThread(() => handler.PageTitle);
                if (pagetitle == null) return null;
                return JsonConvert.SerializeObject(pagetitle);
            }
            [JavascriptInterface, Export("set_PageTitle")]
            set
            {
                handler.DoOnMainThread(() =>
                {
                    if (value == null || value == "(null)" || value == "null") handler.PageTitle = null;
                    else handler.PageTitle = JsonConvert.DeserializeObject<PageTitle>(value);
                });
            }
        }

    }
}

