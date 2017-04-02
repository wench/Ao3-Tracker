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
using Xamarin.Forms.Platform.UWP;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using System.Threading;
using Ao3TrackReader.Data;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.ApplicationModel.DataTransfer;

namespace Ao3TrackReader
{
    public partial class WebViewPage : IWebViewPage, IWebViewPageNative
    {
        public string[] ScriptsToInject { get; } = new[] {
                "jquery-3.1.1.js",
                "marshal.js",
                "platform.js",
                "reader.js",
                "tracker.js",
                "touch.js",
                "unitconv.js"
        };
        public string[] CssToInject { get; } = new[] { "tracker.css" };

        public void CreateWebViewAdditional()
        {
            webView.NewWindowRequested += WebView_NewWindowRequested;

            helper = new Ao3TrackHelper(this);
        }

        private void WebView_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            args.Handled = true;
            var uri = Ao3SiteDataLookup.CheckUri(args.Uri);
            if (uri != null) {
                webView.Navigate(uri);
            }
            else {
                OpenExternal(args.Uri);
            }
        }

        public void AddJavascriptObject(string name, object obj)
        {
            webView.AddWebAllowedObject(name, obj);
        }

        Task OnInjectingScripts(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        Task OnInjectedScripts(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        object IWebViewPage.DoOnMainThread(MainThreadFunc function)
        {
            var task = DoOnMainThreadAsync(() => function());
            task.Wait();
            return task.Result;
        }

        IAsyncOperation<IDictionary<long, WorkChapter>> IWebViewPage.GetWorkChaptersAsync(long[] works)
        {
            return GetWorkChaptersAsync(works).AsAsyncOperation();
        }
        IAsyncOperation<IDictionary<string, bool>> IWebViewPage.AreUrlsInReadingListAsync(string[] urls)
        {
            return AreUrlsInReadingListAsync(urls).AsAsyncOperation();
        }
        IAsyncOperation<string> Helper.IWebViewPage.CallJavascriptAsync(string function, params object[] args)
        {
            return CallJavascriptAsync(function, args).AsAsyncOperation();
        }
        IAsyncOperation<string> Helper.IWebViewPage.EvaluateJavascriptAsync(string code)
        {
            return CallJavascriptAsync(code).AsAsyncOperation();
        }

    }
}
