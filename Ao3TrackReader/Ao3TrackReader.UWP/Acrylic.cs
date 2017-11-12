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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Microsoft.Graphics.Canvas.Effects;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls;

#if WINDOWS_15063
namespace Ao3TrackReader.UWP
{
    public class Acrylic
    {
        Brush brush;
        Color color;
        float opacity;

        public Acrylic(Color color, double opacity = 0.6f)
        {
            this.color = color;
            this.opacity = (float) opacity;

#if WINDOWS_16299
            if (App.UniversalApi >= 5 && Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.AcrylicBrush"))
            {
                var acrylicBrush = new AcrylicBrush();
                Color tint = color;
                tint.A = 0xFF;
                acrylicBrush.TintColor = tint;
                acrylicBrush.TintOpacity = opacity;
                acrylicBrush.FallbackColor = color;
                acrylicBrush.BackgroundSource = AcrylicBackgroundSource.Backdrop;
                brush = acrylicBrush;
            }
#endif
        }

        class GlassCanvas : Canvas
        {
            public GlassCanvas() : base()
            {

            }
        }

        bool TrySetCompositorAcrylic(Panel elem)
        {
            if (App.UniversalApi >= 3 && Windows.Foundation.Metadata.ApiInformation.IsMethodPresent("Windows.UI.Composition.Compositor", "CreateBackdropBrush"))
            {
                GlassCanvas glassHost = elem.Children.FirstOrDefault((c) => c is GlassCanvas) as GlassCanvas;
                if (glassHost != null) elem.Children.Remove(glassHost);
                else glassHost = new GlassCanvas();
                glassHost.Width = elem.ActualWidth;
                glassHost.Height = elem.ActualHeight;
                glassHost.HorizontalAlignment = HorizontalAlignment.Stretch;
                glassHost.VerticalAlignment = VerticalAlignment.Stretch;

                elem.Children.Insert(0, glassHost);

                var hostVisual = ElementCompositionPreview.GetElementVisual(elem);
                var compositor = hostVisual.Compositor;

                Color tint = color;
                tint.A = 0xFF;

                var glassEffect = new GaussianBlurEffect
                {
                    BlurAmount = 30.0f,
                    BorderMode = EffectBorderMode.Hard,
                    Source = new ArithmeticCompositeEffect
                    {
                        MultiplyAmount = 0,
                        Source1Amount = 1f - opacity,
                        Source2Amount = opacity,
                        Source1 = new CompositionEffectSourceParameter("backdropBrush"),
                        Source2 = new ColorSourceEffect
                        {
                            Color = tint
                        }
                    }
                };

                var effectFactory = compositor.CreateEffectFactory(glassEffect);
                var effectBrush = effectFactory.CreateBrush();
                effectBrush.SetSourceParameter("backdropBrush", compositor.CreateBackdropBrush());

                // Create a Visual to contain the frosted glass effect
                var glassVisual = compositor.CreateSpriteVisual();
                glassVisual.Brush = effectBrush;

                // Make sure size of glass host and glass visual always stay in sync
                var bindSizeAnimation = compositor.CreateExpressionAnimation("hostVisual.Size");
                bindSizeAnimation.SetReferenceParameter("hostVisual", hostVisual);

                glassVisual.StartAnimation("Size", bindSizeAnimation);

                ElementCompositionPreview.SetElementChildVisual(glassHost, glassVisual);

                return true;
            }

            return false;
        }

        public bool TrySet(Panel elem)
        {
            if (brush != null)
            {
                elem.Background = brush;
                return true;
            }
            if (TrySetCompositorAcrylic(elem))
            {
                return true;
            }
            return false;

        }

        static Acrylic veryHigh;
        public static Acrylic VeryHigh
        {
            get
            {
                if (veryHigh is null)
                {
                    veryHigh = new Acrylic(Resources.Colors.Alt.Trans.VeryHigh.ToWindows(), 0.5);
                }
                return veryHigh;
            }
        }

        static Acrylic high;
        static public Acrylic High
        {
            get
            {
                if (App.UniversalApi >= 5 && high is null)
                {
                    high = new Acrylic(Resources.Colors.Alt.Trans.High.ToWindows(), 0.5);
                }
                return high;
            }
        }

        static Acrylic mediumHigh;
        static public Acrylic MediumHigh
        {
            get
            {
                if (App.UniversalApi >= 5 && mediumHigh is null)
                {
                    mediumHigh = new Acrylic(Resources.Colors.Alt.Trans.MediumHigh.ToWindows(), 0.5);
                }
                return mediumHigh;
            }
        }
    }
}
#endif
