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

[assembly: ExportRenderer(typeof(TextView), typeof(Ao3TrackReader.UWP.TextViewRenderer))]
namespace Ao3TrackReader.UWP
{
    class TextViewRenderer : LabelRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            base.OnElementChanged(e);

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
        
        void UpdateControl(TextView view)
        {
            if (view.TextTree == null) return;
            Control.TextWrapping = Windows.UI.Xaml.TextWrapping.WrapWholeWords;
            Control.Inlines.Clear();
            Control.Margin = new Windows.UI.Xaml.Thickness(0.0, 0.0, 12.0, 8.0);
            Control.Inlines.Add(view.TextTree.FlattenToSpan());
            Control.InvalidateArrange();
            Control.InvalidateMeasure();
        }
    }
}
