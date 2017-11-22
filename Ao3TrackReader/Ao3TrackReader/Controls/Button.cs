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
using Ao3TrackReader.Models;
using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
    class Button : Xamarin.Forms.Button, Models.IHelpInfo
    {
        public static readonly Xamarin.Forms.BindableProperty ImageHeightProperty =
            BindableProperty.Create("ImageHeight", typeof(double), typeof(Button), defaultValue: 0.0);
        public static readonly Xamarin.Forms.BindableProperty ImageWidthProperty =
            BindableProperty.Create("ImageWidth", typeof(double), typeof(Button), defaultValue: 0.0);
        public static readonly Xamarin.Forms.BindableProperty IsActiveProperty =
            BindableProperty.Create("IsActive", typeof(bool), typeof(Button), defaultValue: false);
        public static readonly BindableProperty PaddingProperty =
            BindableProperty.Create("Padding", typeof(Thickness), typeof(Button), defaultValue: new Thickness(4.0) );

        public double ImageHeight
        {
            get { return (double)GetValue(ImageHeightProperty); }
            set { SetValue(ImageHeightProperty, value); }
        }
        public double ImageWidth
        {
            get { return (double)GetValue(ImageWidthProperty); }
            set { SetValue(ImageWidthProperty, value); }
        }
        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /*
        bool isActive = false;
        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                if (value) BackgroundColor = Ao3TrackReader.Resources.Colors.Highlight.Trans.Medium;
                else BackgroundColor = Color.Transparent;
            }
        }*/

        string IHelpInfo.Text => HelpView.GetText(this);

        Text.TextEx IHelpInfo.Description => HelpView.GetDescription(this);

        FileImageSource IHelpInfo.Icon => Image;

        string IGroupable.Group => HelpView.GetGroup(this);

        string IGroupable.GroupType => null;

        bool IGroupable.ShouldHide => false;
    }
}
