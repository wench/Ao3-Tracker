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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
#if WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;
using XScrollViewRenderer = Xamarin.Forms.Platform.UWP.ScrollViewRenderer;
#else
using Xamarin.Forms.Platform.WinRT;
using XScrollViewRenderer = Xamarin.Forms.Platform.WinRT.ScrollViewRenderer;
#endif
using ScrollOrientation = Xamarin.Forms.ScrollOrientation;
using XScrollView = Xamarin.Forms.ScrollView;
using Ao3TrackReader.Controls;
using Windows.UI.Xaml.Controls;

[assembly: ExportRenderer(typeof(ScrollView), typeof(Ao3TrackReader.ScrollViewRenderer))]
namespace Ao3TrackReader
{
    public class ScrollViewRenderer : XScrollViewRenderer
    {        
        protected override void OnElementChanged(ElementChangedEventArgs<XScrollView> e)
        {
            bool controlWasNull = Control == null;

            base.OnElementChanged(e);

            if (Control != null && controlWasNull)
            {
                Control.ViewChanged += Control_ViewChanged;
                UpdateOrientation();
            }
        }

        private void Control_ViewChanged(object sender, Windows.UI.Xaml.Controls.ScrollViewerViewChangedEventArgs e)
        {
            if (!e.IsIntermediate) (Element as Ao3TrackReader.Controls.ScrollView).SetScrollEnd();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == ScrollView.OrientationProperty.PropertyName || e.PropertyName == ScrollView.HideScrollbarsProperty.PropertyName)
            {
                UpdateOrientation();
            }
        }

        void UpdateOrientation()
        {
            var elem = Element as ScrollView;
            if (elem.Orientation == ScrollOrientation.Horizontal || elem.Orientation == ScrollOrientation.Both)
            {

                Control.HorizontalScrollBarVisibility = elem.HideScrollbars ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Auto;
            }
            else
            {
                Control.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

            if (elem.Orientation == ScrollOrientation.Vertical || elem.Orientation == ScrollOrientation.Both)
            {
                Control.VerticalScrollBarVisibility = elem.HideScrollbars ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Auto;
            }
            else
            {
                Control.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

        }
    }
}
