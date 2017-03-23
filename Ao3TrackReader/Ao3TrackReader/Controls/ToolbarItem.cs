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

namespace Ao3TrackReader.Controls
{
    public sealed class ToolbarItem : Xamarin.Forms.ToolbarItem, Models.IHelpInfo
    {
        public static readonly BindableProperty ForegroundProperty =
          BindableProperty.Create("Foreground", typeof(Color), typeof(ToolbarItem), defaultValue: Color.Default);

        public static readonly BindableProperty DescriptionProperty =
          BindableProperty.Create("Description", typeof(Text.TextEx), typeof(ToolbarItem), defaultValue: null);

        public static readonly BindableProperty IsVisibleProperty =
          BindableProperty.Create("IsVisible", typeof(bool), typeof(ToolbarItem), defaultValue: true);

        public Color Foreground
        {
            get { return (Color)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public Text.TextEx Description
        {
            get { return (Text.TextEx)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }



#if __ANDROID__
        internal Android.Views.IMenuItem MenuItem { get; set; }
#endif

        string IGroupable.Group => "Toolbar";

        string IGroupable.GroupType => null;

        bool IGroupable.ShouldHide => false;
    }
}
