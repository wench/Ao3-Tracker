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
using System.Threading.Tasks;
#if WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;
#else
using Xamarin.Forms.Platform.WinRT;
#endif
using Windows.UI.Xaml.Controls;
using DropDown = Ao3TrackReader.Controls.DropDown;
using System.ComponentModel;
using Xamarin.Forms;
using Windows.Foundation;

[assembly: ExportRenderer(typeof(DropDown), typeof(Ao3TrackReader.WinRT.DropDownRenderer))]
namespace Ao3TrackReader.WinRT
{
    class DropDownRenderer : PickerRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                var b = new Windows.UI.Xaml.Data.Binding()
                {
                    Mode = Windows.UI.Xaml.Data.BindingMode.OneWay,
                    Path = new Windows.UI.Xaml.PropertyPath("FontSize"),
                    Source = e.NewElement
                };
                Control.SetBinding(ComboBox.FontSizeProperty, b);
            }
        }
    }
}
