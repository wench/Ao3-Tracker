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
using System.Runtime.CompilerServices;
using System.Text;
using Ao3TrackReader.Models;
using Xamarin.Forms;
#if WINDOWS_UWP
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.WindowsSpecific;
#endif

namespace Ao3TrackReader
{
    public class NavigationPage : Xamarin.Forms.NavigationPage, IPageEx
    {
        public NavigationPage(WebViewPage page) : base(page)
        {
#if WINDOWS_UWP
            On<Xamarin.Forms.PlatformConfiguration.Windows>().SetToolbarPlacement(ToolbarPlacement.Bottom);
#endif
            BarBackgroundColor = Ao3TrackReader.Resources.Colors.Alt.Solid.MediumHigh;
        }

        protected override bool OnBackButtonPressed()
        {
            if (CurrentPage.SendBackButtonPressed())
                return true;

            return base.OnBackButtonPressed();
        }

        public Text.TextEx TitleEx
        {
            get
            {
                return PageEx.GetTitleEx(this);
            }
        }
    }
}
