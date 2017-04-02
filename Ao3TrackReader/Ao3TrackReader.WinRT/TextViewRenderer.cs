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
using Label = Xamarin.Forms.Label;
#if WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;
#else
using Xamarin.Forms.Platform.WinRT;
#endif
using Windows.UI.Xaml.Documents;
using Ao3TrackReader.Controls;
using Ao3TrackReader.Models;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.Foundation;

[assembly: ExportRenderer(typeof(TextView), typeof(Ao3TrackReader.TextViewRenderer))]
namespace Ao3TrackReader
{
    class TextViewRenderer : LabelRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            if (Control != null)
            {
            }

            base.OnElementChanged(e);

            if (Control != null)
            {
                //Control.Margin = new Windows.UI.Xaml.Thickness(0, 0, 20, 0);
                Control.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
                Control.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Top;
            }

            var view = e.NewElement as TextView;
            if (view != null)
            {
                UpdateControl(view);
            }
        }

        protected override void OnElementPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var view = Element as TextView;

            if (view != null &&
                e.PropertyName == Label.TextProperty.PropertyName ||
                e.PropertyName == Label.FormattedTextProperty.PropertyName ||
                e.PropertyName == TextView.TextExProperty.PropertyName)
            {
                if (view.TextEx != null)
                {
                    UpdateControl(view);
                    return;
                }
            }
            else if (e.PropertyName == Xamarin.Forms.VisualElement.IsVisibleProperty.PropertyName)
            {
                var vec = Element as Xamarin.Forms.IVisualElementController;
                vec.InvalidateMeasure(Xamarin.Forms.Internals.InvalidationTrigger.RendererReady);
            }

            base.OnElementPropertyChanged(sender, e);
        }
        public override Xamarin.Forms.SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            Control.Measure(new Size(widthConstraint,heightConstraint));
            return new Xamarin.Forms.SizeRequest(new Xamarin.Forms.Size(Math.Ceiling(Control.DesiredSize.Width), Math.Ceiling(Control.DesiredSize.Height)));
        }

        void UpdateControl(TextView view)
        {
            if (view.TextEx == null) return;
            Control.TextWrapping = Windows.UI.Xaml.TextWrapping.WrapWholeWords;
            Control.Inlines.Clear();
            Control.Inlines.Add(view.TextEx.FlattenToSpan());
            (Element as Xamarin.Forms.IVisualElementController).InvalidateMeasure(Xamarin.Forms.Internals.InvalidationTrigger.RendererReady);
            Control.InvalidateMeasure();
            InvalidateMeasure();
        }
    }
}
