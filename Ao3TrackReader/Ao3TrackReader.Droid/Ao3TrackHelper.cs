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
            get { return JsonConvert.SerializeObject(handler.scriptsToInject); }
        }

        public string CssToInject
        {
            [JavascriptInterface, Export("get_CssToInject")]
            get { return JsonConvert.SerializeObject(handler.cssToInject); }
        }

        public int JumpToLastLocationEvemt
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
                if (JumpToLastLocationEvemt != 0)
                    handler.CallJavascript("Ao3TrackCallbacks.Call" + JumpToLastLocationEvemt, pagejump);
            });
        }
        public bool jumpToLastLocationEnabled
        {
            [JavascriptInterface, Export("get_JumpToLastLocationEnabled")]
            get { return (bool)handler.DoOnMainThread(() => handler.JumpToLastLocationEnabled); }
            [JavascriptInterface, Export("set_JumpToLastLocationEnabled")]
            set { handler.DoOnMainThread(() => handler.JumpToLastLocationEnabled = value); }
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

        public int fontSize
        {
            [JavascriptInterface, Export("get_FontSize")]
            get { return (int)handler.DoOnMainThread(() => handler.FontSize); }
            [JavascriptInterface, Export("set_FontSize")]
            set { handler.DoOnMainThread(() => handler.FontSize = value); }
        }

        public void Reset()
        {
            JumpToLastLocationEvemt = 0;
            AlterFontSizeEvent = 0;
            CurrentWorkId = 0;
            CurrentLocation = null;
        }


        [JavascriptInterface, Export("getWorkChaptersAsync")]
        public void getWorkChaptersAsync(string works_json, int hCallback)
        {
            Task.Run(async () =>
            {
                var works = JsonConvert.DeserializeObject<long[]>(works_json);
                var workchapters = await handler.GetWorkChaptersAsync(works);
                handler.CallJavascript("Ao3TrackCallbacks.Call", hCallback, JsonConvert.SerializeObject(workchapters).ToLiteral());
            });
        }

        [JavascriptInterface, Export("setWorkChapters")]
        public void setWorkChapters(string workchapters_json)
        {
            Task.Run(() =>
            {
                var workchapters = JsonConvert.DeserializeObject<Dictionary<long, WorkChapter>>(workchapters_json);
                handler.SetWorkChapters(workchapters);
            });
        }

        [JavascriptInterface, Export("showContextMenu")]
        public void showContextMenu(double x, double y, string menuItems_json, int hCallback)
        {
            Task.Run(async () =>
            {
                var menuItems = JsonConvert.DeserializeObject<string[]>(menuItems_json);
                string result = await handler.showContextMenu(x, y, menuItems);
                handler.CallJavascript("Ao3TrackCallbacks.Call", hCallback, result.ToLiteral());
            });
        }

        [JavascriptInterface, Export("addToReadingList")]
        public void addToReadingList(string href)
        {
            handler.addToReadingList(href);
        }
        [JavascriptInterface, Export("copyToClipboard")]
        public void copyToClipboard(string str, string type)
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
        public void setCookies(string cookies)
        {
            handler.setCookies(cookies);
        }


        public string nextPage
        {
            [JavascriptInterface, Export("get_NextPage")]
            get { return handler.NextPage; }
            [JavascriptInterface, Export("set_NextPage")]
            set { handler.DoOnMainThread(() => handler.NextPage = value); }
        }
        public string prevPage
        {
            [JavascriptInterface, Export("get_PrevPage")]
            get { return handler.PrevPage; }
            [JavascriptInterface, Export("set_PrevPage")]
            set { handler.DoOnMainThread(() => handler.PrevPage = value); }
        }

        public bool canGoBack
        {
            [JavascriptInterface, Export("get_CanGoBack")]
            get { return (bool)handler.DoOnMainThread(() => handler.canGoBack); }
        }
        public bool canGoForward
        {
            [JavascriptInterface, Export("get_CanGoForward")]
            get { return (bool)handler.DoOnMainThread(() => handler.canGoForward); }
        }

        [JavascriptInterface, Export("goBack")]
        public void goBack() { handler.DoOnMainThread(() => handler.GoBack()); }

        [JavascriptInterface, Export("goForward")]
        public void goForward() { handler.DoOnMainThread(() => handler.GoForward()); }

        public double leftOffset
        {
            [JavascriptInterface, Export("get_LeftOffset")]
            get { return (double)handler.DoOnMainThread(() => handler.leftOffset); }
            [JavascriptInterface, Export("set_LeftOffset")]
            set { handler.DoOnMainThread(() => handler.leftOffset = value); }
        }
        public double opacity
        {
            [JavascriptInterface, Export("get_Opacity")]
            get { return (double)handler.DoOnMainThread(() => handler.opacity); }
            [JavascriptInterface, Export("set_Opacity")]
            set { handler.DoOnMainThread(() => handler.opacity = value); }
        }
        public bool showPrevPageIndicator
        {
            [JavascriptInterface, Export("get_ShowPrevPageIndicator")]
            get { return (bool)handler.DoOnMainThread(() => handler.showPrevPageIndicator); }
            [JavascriptInterface, Export("set_ShowPrevPageIndicator")]
            set { handler.DoOnMainThread(() => handler.showPrevPageIndicator = value); }
        }
        public bool showNextPageIndicator
        {
            [JavascriptInterface, Export("get_ShowNextPageIndicator")]
            get { return (bool)handler.DoOnMainThread(() => handler.showNextPageIndicator); }
            [JavascriptInterface, Export("set_ShowNextPageIndicator")]
            set { handler.DoOnMainThread(() => handler.showNextPageIndicator = value); }
        }


        public string CurrentLocation {
            [JavascriptInterface, Export("get_CurrentLocation")]
            get
            {
                var loc = handler.CurrentLocation;
                if (loc == null) return null;
                return JsonConvert.SerializeObject(loc);
            }
            [JavascriptInterface, Export("set_CurrentLocation")]
            set
            {
                if (value == null || value == "(null)" || value == "null")
                    handler.CurrentLocation = null;
                else
                    handler.CurrentLocation = JsonConvert.DeserializeObject<WorkChapter>(value);
            }
        }

    }
}

