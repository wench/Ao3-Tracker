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
using Windows.UI.Xaml.Media;

#if WINDOWS_16299
[assembly: Xamarin.Forms.Platform.UWP.ExportRenderer(typeof(Ao3TrackReader.Controls.PaneView), typeof(Ao3TrackReader.UWP.PaneViewRenderer))]
namespace Ao3TrackReader.UWP
{
    class PaneViewRenderer : Xamarin.Forms.Platform.UWP.LayoutRenderer
    {
        protected override void UpdateBackgroundColor()
        {
            if (App.UniversalApi >= 5)
            {
                XamlCompositionBrushBase acrylic = Acrylic.VeryHigh;
                if (acrylic != null)
                {
                    Background = acrylic;
                    return;
                }
            }

            base.UpdateBackgroundColor();
        }

    }
}
#endif
