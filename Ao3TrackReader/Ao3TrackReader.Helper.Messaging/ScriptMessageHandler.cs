using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Helper.Messaging
{
    public class ScriptMessageHandler
    {
        WebViewPage wvp;
        private Ao3TrackHelper helper;

        protected ScriptMessageHandler(WebViewPage wvp, Ao3TrackHelper helper)
        {
            this.wvp = wvp;
            this.helper = helper;
        }

        public class Message
        {
            public string type { get; set; }
            public string name { get; set; }
            public string value { get; set; }
            public string[] args { get; set; }
        }

        private object Deserialize(string value, Type type)
        {
            // If destination is a string, then the value passes through unchanged. A minor optimization
            if (type == typeof(string)) return value;
            else return JsonConvert.DeserializeObject(value, type);
        }

        protected void HandleMessage(string message)
        {
            var msg = JsonConvert.DeserializeObject<Message>(message);

            if (msg.type == "INIT")
            {
                wvp.DoOnMainThread(async () =>
                {
                    await wvp.EvaluateJavascriptAsync(string.Format(
                        "Ao3Track.Messaging.helper.setValue({0},{1});" +
                        "Ao3Track.Messaging.helper.setValue({2},{3});" +
                        "Ao3Track.Messaging.helper.setValue({4},{5});" +
                        "Ao3Track.Messaging.helper.setValue({6},{7});",
                        JsonConvert.SerializeObject("leftOffset"), JsonConvert.SerializeObject(wvp.LeftOffset),
                        JsonConvert.SerializeObject("swipeCanGoBack"), JsonConvert.SerializeObject(wvp.SwipeCanGoBack),
                        JsonConvert.SerializeObject("swipeCanGoForward"), JsonConvert.SerializeObject(wvp.SwipeCanGoForward),
                        JsonConvert.SerializeObject("deviceWidth"), JsonConvert.SerializeObject(wvp.DeviceWidth)));
                });
                return;
            }
            else if (helper.HelperDef.TryGetValue(msg.name, out var md))
            {
                if (msg.type == "SET" && md.pi?.CanWrite == true)
                {
                    md.pi.SetValue(helper, Deserialize(msg.value, md.pi.PropertyType));
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
                            args[i] = Deserialize(msg.args[i], ps[i].ParameterType);
                        }

                        md.mi.Invoke(helper, args);
                        return;
                    }
                }
            }

            throw new ArgumentException();
        }
    }
}
