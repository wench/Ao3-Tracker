using System;
using System.Collections.Generic;
using System.Text;
using Ao3TrackReader.Helper;
using WebKit;

namespace Ao3TrackReader.iOS
{
    public class ScriptMessageHandler : Ao3TrackReader.Helper.Messaging.ScriptMessageHandler
    {
        class NativeHandler : WKScriptMessageHandler
        {
            ScriptMessageHandler smg;

            public NativeHandler(ScriptMessageHandler smg)
            {
                this.smg = smg;
            }

            public override void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
            {
                smg.HandleMessage(message.Body.ToString());
            }
        }

        public ScriptMessageHandler(Ao3TrackHelper helper) : base(helper)
        {
        }

        public static explicit operator WKScriptMessageHandler(ScriptMessageHandler toCast)
        {
            return new NativeHandler(toCast);
        }
    }
}
