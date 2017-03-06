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
            Task<object>.Run(async () =>
            {
                if (onjumptolastlocationevent != 0) await wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", onjumptolastlocationevent, pagejump);
            });
        }


        int _onalterfontsizeevent;
        public int onalterfontsizeevent
        {
            private get { return _onalterfontsizeevent; }
            [Converter("Event")]
            set
            {
                if (value != 0) wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", value, wvp.FontSize).Wait(0);
                _onalterfontsizeevent = value;
            }
        }

        void IAo3TrackHelper.OnAlterFontSize(int fontSize)
        {
            Task<object>.Run(async () =>
            {
                if (onalterfontsizeevent != 0) await wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", onalterfontsizeevent, fontSize);
            });
        }


        public void GetWorkChaptersAsync(long[] works, [Converter("Callback")] int hCallback)
        {
            Task.Run(async () =>
            {
                var workchapters = await wvp.GetWorkChaptersAsync(works);
                wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", hCallback, workchapters).Wait(0);
            });
        }

        public void SetWorkChapters(Dictionary<long, WorkChapter> workchapters)
        {
            Task.Run(() =>
            {
                wvp.SetWorkChapters(workchapters);
            });
        }

        public void HideContextMenu()
        {
            wvp.HideContextMenu();
        }

        public void ShowContextMenu(double x, double y, string[] menuItems, [Converter("Callback")] int hCallback)
        {
            Task.Run(async () =>
            {
                string result = await wvp.ShowContextMenu(x, y, menuItems);
                await wvp.CallJavascriptAsync("Ao3Track.Callbacks.Call", hCallback, result);
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
            set { wvp.DoOnMainThread(() => {
                wvp.NextPage = value;
                wvp.CallJavascriptAsync("Ao3Track.iOS.helper.setValue", "canGoForward", wvp.CanGoForward).Wait(0);
            }); }

        }
        public string PrevPage
        {
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
