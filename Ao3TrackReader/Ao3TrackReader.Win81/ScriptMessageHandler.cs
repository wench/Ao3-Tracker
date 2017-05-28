using System;
using System.Collections.Generic;
using System.Text;
using Ao3TrackReader.Helper;
using Windows.UI.Xaml.Controls;

namespace Ao3TrackReader.Win81
{
    public class ScriptMessageHandler : Ao3TrackReader.Helper.Messaging.ScriptMessageHandler
    {
        public ScriptMessageHandler(Ao3TrackHelper helper) : base(helper)
        {
        }

        public void WebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            HandleMessage(e.Value);
        }
    }
}
