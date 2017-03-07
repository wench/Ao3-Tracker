﻿/*
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
using System.Text;
using System.Threading.Tasks;

namespace Ao3TrackReader
{
    // This is the stuff that the native part of the WebViewPage must implement
    interface IWebViewPageNative
    {
        bool IsMainThread { get; }

        bool ShowBackOnToolbar { get; }

        Xamarin.Forms.View CreateWebView();
        Uri CurrentUri { get; }
        void Navigate(Uri uri);
        void Refresh();
        bool SwipeCanGoBack { get; }
        bool SwipeCanGoForward { get; }
        void SwipeGoBack();
        void SwipeGoForward();
        Task<string> EvaluateJavascriptAsync(string code);
        double LeftOffset { get; set; }

        void HideContextMenu();
        Task<string> ShowContextMenu(double x, double y, string[] menuItems);
    }
}
