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
using Windows.UI;
using Windows.UI.Xaml.Media;
using System.Reflection;

#if WINDOWS_FUTURE
namespace Ao3TrackReader.UWP
{
    public static class Acrylic
    {
        static void LoadAcrylicBrush(Xamarin.Forms.Color color, double opacity, ref XamlCompositionBrushBase brush)
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.AcrylicBrush"))
            {
                var acrylicBrush = new Windows.UI.Xaml.Media.AcrylicBrush();
                Color tint = color.ToWindows();
                tint.A = 0xFF;
                acrylicBrush.TintColor = tint;
                acrylicBrush.TintOpacity = opacity;
                acrylicBrush.FallbackColor = color.ToWindows();
                acrylicBrush.BackgroundSource = AcrylicBackgroundSource.Backdrop;
                brush = acrylicBrush;
            }
        }

        static XamlCompositionBrushBase veryHigh;
        static public XamlCompositionBrushBase VeryHigh
        {
            get
            {
                if (App.UniversalApi >= 5 && veryHigh is null)
                {
                    LoadAcrylicBrush(Resources.Colors.Alt.Trans.VeryHigh, 0.6, ref veryHigh);
                }
                return veryHigh;
            }
        }

        static XamlCompositionBrushBase high;
        static public XamlCompositionBrushBase High
        {
            get
            {
                if (App.UniversalApi >= 5 && high is null)
                {
                    LoadAcrylicBrush(Resources.Colors.Alt.Trans.High, 0.6, ref high);
                }
                return high;
            }
        }

        static XamlCompositionBrushBase mediumHigh;
        static public XamlCompositionBrushBase MediumHigh
        {
            get
            {
                if (App.UniversalApi >= 5 && mediumHigh is null)
                {
                    LoadAcrylicBrush(Resources.Colors.Alt.Trans.MediumHigh, 0.6, ref mediumHigh);
                }
                return mediumHigh;
            }
        }
    }
}
#endif
