using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebKit;

namespace Ao3TrackReader.Helper
{
    public class Ao3TrackHelper : WKScriptMessageHandler, IAo3TrackHelper
    {
        static string s_memberDef;
        static HelperDef s_def;
        IWebViewPage wvp;

        public Ao3TrackHelper(IWebViewPage wvp)
        {
            this.wvp = wvp;
        }

        class Message
        {
            public string type;
            public string name;
            public string value;
            public string[] args;
        }

        [DefIgnore]
        public override void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
        {
            var smsg = message.Body.ToString();
            var msg = JsonConvert.DeserializeObject<Message>(smsg);
            if (HelperDef.TryGetValue(msg.name, out var md))
            {
                if (msg.type == "SET" && md.pi?.CanWrite == true)
                {
                    md.pi.SetValue(this, JsonConvert.DeserializeObject(msg.value, md.pi.PropertyType));
                    return;
                }
                else if (msg.type == "CALL" && md.mi != null)
                {
                    var ps = md.mi.GetParameters();
                    if (msg.args.Length == ps.Length)
                    {
                        var args = new object[msg.args.Length];
                        for (int i = 0; i < msg.args.Length; i++)
                        {
                            args[i] = JsonConvert.DeserializeObject(msg.args[i], ps[i].ParameterType);
                        }

                        md.mi.Invoke(this, args);
                        return;
                    }
                }
            }

            throw new ArgumentException();
        }

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


        [DefIgnore]
        public string MemberDef
        {
            get
            {
                if (s_memberDef == null)
                {
                    s_memberDef = HelperDef.Serialize();
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
            get
            {
                throw new NotSupportedException();
            }
            [Converter("Event")]
            set
            {
                _onjumptolastlocationevent = value;
                wvp.DoOnMainThread(() => { wvp.JumpToLastLocationEnabled = value != 0; });
            }
        }
        void IAo3TrackHelper.OnJumpToLastLocation(bool pagejump)
        {
            Task<object>.Run(() =>
            {
                if (onjumptolastlocationevent != 0)
                    wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", onjumptolastlocationevent, pagejump).Wait(0);
            });
        }


        int _onalterfontsizeevent;
        public int onalterfontsizeevent
        {
            get
            {
                throw new NotSupportedException();
            }
            [Converter("Event")]
            set
            {
                _onalterfontsizeevent = value;
            }
        }

        void IAo3TrackHelper.OnAlterFontSize(int fontSize)
        {
            Task<object>.Run(() =>
            {
                if (onalterfontsizeevent != 0)
                    wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", onalterfontsizeevent, fontSize).Wait(0);
            });
        }


        public void GetWorkChaptersAsync([Converter("ToJSON")] string works_json, [Converter("Callback")] int hCallback)
        {
            Task.Run(async () =>
            {
                var works = JsonConvert.DeserializeObject<long[]>(works_json);
                var workchapters = await wvp.GetWorkChaptersAsync(works);
                wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", hCallback, workchapters).Wait(0);
            });
        }

        public void SetWorkChapters([Converter("ToJSON")] string workchapters_json)
        {
            Task.Run(() =>
            {
                var workchapters = JsonConvert.DeserializeObject<Dictionary<long, WorkChapter>>(workchapters_json);
                wvp.SetWorkChapters(workchapters);
            });
        }

        public void HideContextMenu()
        {
            wvp.HideContextMenu();
        }

        public void ShowContextMenu(double x, double y, [Converter("ToJSON")] string menuItems_json, [Converter("Callback")] int hCallback)
        {
            Task.Run(async () =>
            {
                var menuItems = JsonConvert.DeserializeObject<string[]>(menuItems_json);
                string result = await wvp.ShowContextMenu(x, y, menuItems);
                wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", hCallback, result).Wait(0);
            });
        }


        public void AddToReadingList(string href)
        {
            wvp.AddToReadingList(href);
        }

        public void CopyToClipboard(string str, string type)
        {
            /*
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
            }*/
        }

        public void SetCookies(string cookies)
        {
            wvp.SetCookies(cookies);
        }

        public string NextPage
        {
            get { throw new NotSupportedException(); }
            set { wvp.DoOnMainThread(() => {
                wvp.NextPage = value;
                wvp.CallJavascriptAsync("Ao3Track.iOS.helper.setValue", "canGoForward", wvp.CanGoForward).Wait(0);
            }); }

        }
        public string PrevPage
        {
            get { throw new NotSupportedException(); }
            set { wvp.DoOnMainThread(() => {
                wvp.PrevPage = value;
                wvp.CallJavascriptAsync("Ao3Track.iOS.helper.setValue", "canGoBack", wvp.CanGoBack).Wait(0);
            }); }
        }

        public bool CanGoBack
        {
            get { throw new NotSupportedException(); }
        }
        public bool CanGoForward
        {
            get { throw new NotSupportedException(); }
        }

        public void GoBack() { wvp.DoOnMainThread(() => wvp.GoBack()); }

        public void GoForward() { wvp.DoOnMainThread(() => wvp.GoForward()); }

        public double LeftOffset
        {
            get { throw new NotSupportedException(); }
            set { wvp.DoOnMainThread(() => { wvp.LeftOffset = value; }); }
        }
        public int ShowPrevPageIndicator
        {
            get { throw new NotSupportedException(); }
            set { wvp.DoOnMainThread(() => { wvp.ShowPrevPageIndicator = value; }); }
        }
        public int ShowNextPageIndicator
        {
            get { throw new NotSupportedException(); }
            set { wvp.DoOnMainThread(() => { wvp.ShowNextPageIndicator = value; }); }
        }

        public string CurrentLocation
        {
            [Converter("FromJSON")]
            get
            {
                throw new NotSupportedException();
            }
            [Converter("ToJSON")]
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
            [Converter("FromJSON")]
            get
            {
                throw new NotSupportedException();
            }
            [Converter("ToJSON")]
            set
            {
                wvp.DoOnMainThread(() =>
                {
                    if (value == null || value == "(null)" || value == "null") wvp.PageTitle = null;
                    else wvp.PageTitle = JsonConvert.DeserializeObject<PageTitle>(value);
                });
            }
        }

        public void AreUrlsInReadingListAsync([Converter("ToJSON")] string urls_json, [Converter("Callback")] int hCallback)
        {
            Task.Run(async () =>
            {
                var urls = JsonConvert.DeserializeObject<string[]>(urls_json);
                var res = await wvp.AreUrlsInReadingListAsync(urls);
                wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", hCallback, res).Wait(0);
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
