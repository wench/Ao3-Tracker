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
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Ao3TrackReader.UWP
{
    public class FormsCommandBar : Xamarin.Forms.Platform.UWP.FormsCommandBar
    {
        public FormsCommandBar() : base()
        {
            IsDynamicOverflowEnabled = true;
            PrimaryCommands.VectorChanged += VectorChanged;
            SecondaryCommands.VectorChanged += VectorChanged;
        }

        private void VectorChanged(Windows.Foundation.Collections.IObservableVector<ICommandBarElement> sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemInserted)
            {
                var item = sender[(int) args.Index] as AppBarButton;
                if (item == null) return;
                var xitem = item.DataContext as Xamarin.Forms.ToolbarItem;
                if (xitem == null) return;
                item.ClearValue(AppBarButton.IconProperty);
                var uri = new Uri("ms-appx:///" + xitem.Icon.File);
                item.Icon = new BitmapIcon() { UriSource = uri};

                if (item.DataContext is Ao3TrackReader.Controls.ToolbarItem aitem)
                {
                    // Create the binding description.
                    Binding b = new Binding()
                    {
                        Mode = BindingMode.OneWay,
                        Path = new PropertyPath("Foreground"),
                        Converter = new ColorConverter()
                    };
                    item.SetBinding(AppBarButton.ForegroundProperty, b);
                }
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size res = availableSize;
            try
            {
                res = base.MeasureOverride(availableSize);
            }
            catch(System.Runtime.InteropServices.COMException)
            {
                res.Height = 48;
            }
            return res;
        }
    }
}
