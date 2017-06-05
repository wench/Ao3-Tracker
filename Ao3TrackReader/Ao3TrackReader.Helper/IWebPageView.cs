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
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;

#if WINDOWS_UWP
using Windows.Foundation;
using Windows.Foundation.Metadata;
using IAsyncOp_WorkWorkDetailsMap = Windows.Foundation.IAsyncOperation<System.Collections.Generic.IDictionary<long, Ao3TrackReader.Helper.IWorkDetails>>;
using IAsyncOp_StringBoolMap = Windows.Foundation.IAsyncOperation<System.Collections.Generic.IDictionary<string, bool>>;
using IAsyncOp_String = Windows.Foundation.IAsyncOperation<string>;
#else 
using System.Threading.Tasks;
using IAsyncOp_WorkWorkDetailsMap = System.Threading.Tasks.Task<System.Collections.Generic.IDictionary<long, Ao3TrackReader.Helper.IWorkDetails>>;
using IAsyncOp_StringBoolMap = System.Threading.Tasks.Task<System.Collections.Generic.IDictionary<string, bool>>;
using IAsyncOp_String = System.Threading.Tasks.Task<string>;
#endif


namespace Ao3TrackReader.Helper
{
#if WINDOWS_UWP
    [AllowForWeb]
#endif
    public sealed class PageTitle
    {
        public string Title { get; set; }
        public string Chapter { get; set; }
        public string Chaptername { get; set; }
        public string[] Authors { get; set; }
        public string[] Fandoms { get; set; }
        public string Primarytag { get; set; }
    }


    public delegate void MainThreadAction();

#if WINDOWS_UWP
    public delegate object MainThreadFunc();
    public delegate IAsyncOperation<object> MainThreadAsyncFunc();
    public delegate IAsyncAction MainThreadAsyncAction();
#endif

    public interface IWebViewPage
    {
#if WINDOWS_UWP
        [DefaultOverload]
        IAsyncOperation<object> DoOnMainThreadAsync(MainThreadFunc function);
        IAsyncOperation<object> DoOnMainThreadAsync(MainThreadAsyncFunc function);
        IAsyncAction DoOnMainThreadAsync(MainThreadAction function);
        IAsyncAction DoOnMainThreadAsync(MainThreadAsyncAction function);
#else
        Task<T> DoOnMainThreadAsync<T>(Func<T> function);
        Task<T> DoOnMainThreadAsync<T>(Func<Task<T>> function);
        Task DoOnMainThreadAsync(MainThreadAction function);
        Task DoOnMainThreadAsync(Func<Task> function);
#endif

        double DeviceWidth { get; }

        void JavascriptError(string name, string message, string url, int lineNo, int coloumNo, string stack);

        IAsyncOp_WorkWorkDetailsMap GetWorkDetailsAsync([ReadOnlyArray] long[] works, WorkDetailsFlags flags);
        void SetWorkChaptersAsync(IDictionary<long, WorkChapter> works);
        bool JumpToLastLocationEnabled { get; set; }
        string NextPage { get; set; }
        string PrevPage { get; set; }
        bool SwipeCanGoBack { get; }
        bool SwipeCanGoForward { get; }
        void SwipeGoBack();
        void SwipeGoForward();
        double LeftOffset { get; set; }
        int ShowPrevPageIndicator { get; set; }
        int ShowNextPageIndicator { get; set; }
        int FontSize { get; }
        IWorkChapterEx CurrentLocation { get; set; }
        PageTitle PageTitle { get; set; }
        void ShowContextMenu(double x, double y, string url, string innerText);
        void AddToReadingList(string href);
        void SetCookies(string cookies);

        IAsyncOp_StringBoolMap AreUrlsInReadingListAsync([ReadOnlyArray] string[] urls);

        void StartWebViewDragAccelerate(double velocity);
        void StopWebViewDragAccelerate();

        IAsyncOp_String CallJavascriptAsync(string function, params object[] args);
        IAsyncOp_String EvaluateJavascriptAsync(string code);

        ISettings Settings{ get; }

        IAsyncOp_String ShouldFilterWorkAsync(long workId, IEnumerable<string> workauthors, IEnumerable<string> worktags, IEnumerable<long> workserieses);

        bool HasSpeechText { get; set; }
        void SetSpeechText(SpeechText speechText);
    }
}
