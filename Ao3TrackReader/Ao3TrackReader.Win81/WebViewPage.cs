/*
Copyright 2017 Alexis Ryan

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ao3TrackReader.Helper;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Xamarin.Forms.Platform.WinRT;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using System.Threading;
using Ao3TrackReader.Data;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.ApplicationModel.DataTransfer;
using Newtonsoft.Json;

namespace Ao3TrackReader
{
    public partial class WebViewPage : IWebViewPage, IWebViewPageNative
    {
        Xamarin.Forms.View contextMenuPlaceholder;


        public void CreateWebViewAdditional()
        {
            var helper = new Ao3TrackHelper(this);
            var messageHandler = new Win81.ScriptMessageHandler(helper);
            webView.ScriptNotify += messageHandler.WebView_ScriptNotify;
            this.helper = helper;

            contextMenuPlaceholder = new Xamarin.Forms.ContentView();
            Xamarin.Forms.AbsoluteLayout.SetLayoutBounds(contextMenuPlaceholder, new Xamarin.Forms.Rectangle(0, 0, 0, 0));
            Xamarin.Forms.AbsoluteLayout.SetLayoutFlags(contextMenuPlaceholder, Xamarin.Forms.AbsoluteLayoutFlags.PositionProportional);
            MainContent.Children.Insert(0, contextMenuPlaceholder);

            webView.SizeChanged += WebView_SizeChanged;
        }

        private async void WebView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await CallJavascriptAsync("Ao3Track.Messaging.helper.setValue", "deviceWidth", DeviceWidth).ConfigureAwait(false);
        }

        public void AddJavascriptObject(string name, object obj)
        {
        }

        async Task OnInjectingScripts(CancellationToken ct)
        {
            await EvaluateJavascriptAsync("window.Ao3TrackHelperNative = " + helper.HelperDefJson + ";").ConfigureAwait(false);
        }

        Task OnInjectedScripts(CancellationToken ct)
        {
            return Task.FromResult(new object());
        }

    }
}
