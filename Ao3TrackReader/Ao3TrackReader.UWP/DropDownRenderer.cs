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
using Xamarin.Forms.Platform.UWP;
using Windows.UI.Xaml.Controls;
using DropDown = Ao3TrackReader.Controls.DropDown;
using System.ComponentModel;
using Xamarin.Forms;
using Windows.Foundation;

[assembly: ExportRenderer(typeof(DropDown), typeof(Ao3TrackReader.UWP.DropDownRenderer))]
namespace Ao3TrackReader.UWP
{
    class DropDownRenderer : ViewRenderer<DropDown, ComboBox>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<DropDown> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                SetNativeControl(new Windows.UI.Xaml.Controls.ComboBox());
                Control.SelectionChanged += Control_SelectionChanged;
            }

            if (e.OldElement is DropDown old)
            {
                old.SelectedIndexChanged -= Element_ItemSelected;
                Control.ItemsSource = null;
            }

            if (e.NewElement != null)
            {
                Control.ItemsSource = Element.ItemsSource;
                Control.SelectedItem = Element.SelectedItem;
                Element.SelectedIndexChanged += Element_ItemSelected;
            }
        }

        private void Element_ItemSelected(object sender, EventArgs e)
        {
            if (Control.SelectedItem != Element.SelectedItem)
                Control.SelectedItem = Element.SelectedItem;
        }

        private void Control_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Element != null && Element.SelectedItem != Control.SelectedItem)
                Element.SelectedItem = Control.SelectedItem;
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == DropDown.ItemsSourceProperty.PropertyName)
            {
                Control.ItemsSource = Element.ItemsSource;
            }
        }
    }
}
