using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Label = Xamarin.Forms.Label;
using Xamarin.Forms.Platform.UWP;
using Windows.UI.Xaml.Documents;
using Ao3TrackReader.Controls;
using Ao3TrackReader.Models;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.Foundation;

[assembly: ExportRenderer(typeof(TextView), typeof(Ao3TrackReader.UWP.TextViewRenderer))]
namespace Ao3TrackReader.UWP
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
                e.PropertyName == TextView.TextTreeProperty.PropertyName)
            {
                if (view.TextTree != null)
                {
                    UpdateControl(view);
                    return;
                }
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
            if (view.TextTree == null) return;
            Control.TextWrapping = Windows.UI.Xaml.TextWrapping.WrapWholeWords;
            Control.Inlines.Clear();
            Control.Inlines.Add(view.TextTree.FlattenToSpan());
            (Element as Xamarin.Forms.IVisualElementController).InvalidateMeasure(Xamarin.Forms.Internals.InvalidationTrigger.RendererReady);
            Control.InvalidateMeasure();
            InvalidateMeasure();
        }
    }
}
