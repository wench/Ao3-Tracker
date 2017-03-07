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
using Xamarin.Forms;
using Ao3TrackReader.Models;

namespace Ao3TrackReader
{
    public class PageEx : BindableObject
    {
        public static readonly BindableProperty TitleExProperty =
          BindableProperty.CreateAttached("TitleEx", typeof(TextTree), typeof(NavigationPage), null);

        public static TextTree GetTitleEx(BindableObject view)
        {
            return (TextTree)view.GetValue(TitleExProperty);
        }

        public static void SetTitleEx(BindableObject view, Models.TextTree value)
        {
            view.SetValue(TitleExProperty, value);
        }
    }

    public interface IPageEx
    {
        TextTree TitleEx { get; }
        string Title { get; set; }
    }

}
