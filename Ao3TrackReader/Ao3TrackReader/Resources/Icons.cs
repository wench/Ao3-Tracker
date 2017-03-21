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

namespace Ao3TrackReader.Resources
{
    public static class Icons
    {
        public static string Close
        {
            get { return App.PlatformIcon("close") + ".png"; }
        }
        public static string Refresh
        {
            get { return App.PlatformIcon("refresh") + ".png"; }
        }
        public static string Filter
        {
            get { return App.PlatformIcon("filter") + ".png"; }
        }
        public static string AddPage
        {
            get { return App.PlatformIcon("addpage") + ".png"; }
        }
        public static string Bookmarks
        {
            get { return App.PlatformIcon("bookmarks") + ".png"; }
        }
        public static string Font
        {
            get { return App.PlatformIcon("font") + ".png"; }
        }
        public static string FontDown
        {
            get { return App.PlatformIcon("fontdown") + ".png"; }
        }
        public static string FontUp
        {
            get { return App.PlatformIcon("fontup") + ".png"; }
        }
        public static string Settings
        {
            get { return App.PlatformIcon("settings") + ".png"; }
        }
        public static string Rename
        {
            get { return App.PlatformIcon("rename") + ".png"; }
        }
        public static string Sync
        {
            get { return App.PlatformIcon("sync") + ".png"; }
        }
        public static string Forward
        {
            get { return App.PlatformIcon("forward") + ".png"; }
        }
        public static string Back
        {
            get { return App.PlatformIcon("back") + ".png"; }
        }
        public static string Redo
        {
            get { return App.PlatformIcon("redo") + ".png"; }
        }
        public static string Tag
        {
            get { return App.PlatformIcon("tag") + ".png"; }
        }
        public static string ForceLoc
        {
            get { return App.PlatformIcon("forceloc") + ".png"; }
        }
        public static string Help
        {
            get { return App.PlatformIcon("help") + ".png"; }
        }
        public static string More
        {
            get { return App.PlatformIcon("more") + ".png"; }
        }
        public static string SwipeLeft
        {
            get { return App.PlatformIcon("swipeleft") + ".png"; }
        }
        public static string SwipeRight
        {
            get { return App.PlatformIcon("swiperight") + ".png"; }
        }
        public static string Tap
        {
            get { return App.PlatformIcon("tap") + ".png"; }
        }
        public static string TapHold
        {
            get { return App.PlatformIcon("taphold") + ".png"; }
        }
    }
}
