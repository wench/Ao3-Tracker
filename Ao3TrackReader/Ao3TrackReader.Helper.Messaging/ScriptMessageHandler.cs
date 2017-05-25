using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Helper.Messaging
{
    public class ScriptMessageHandler
    {
        private Ao3TrackHelper helper;

        protected ScriptMessageHandler(Ao3TrackHelper helper)
        {
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

            if (helper.HelperDef.TryGetValue(msg.name, out var md))
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
