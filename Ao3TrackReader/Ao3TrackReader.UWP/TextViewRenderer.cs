using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Label = Xamarin.Forms.Label;
using Xamarin.Forms.Platform.UWP;
using Windows.UI.Xaml.Documents;
using Ao3TrackReader.Controls;
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

            var inlines = ConvertTree(view.TextTree);

            Control.Inlines.Clear();
            if (inlines != null) Control.Inlines.Add(inlines);            
            
        }


        void ApplyStyles(TextTree n, Inline i)
        {
            if (n.Bold == true) i.FontWeight = FontWeights.Bold;
            else if (n.Bold == false) i.FontWeight = FontWeights.Normal;

            if (n.Italic == true) i.FontStyle = FontStyle.Italic;
            else if (n.Italic == false) i.FontStyle = FontStyle.Normal;

            if (n.FontSize != null) i.FontSize = (double)n.FontSize;

            if (n.Foreground != null) i.Foreground = new SolidColorBrush(
                Color.FromArgb(
                    (byte)(n.Foreground.Value.A * 255),
                    (byte)(n.Foreground.Value.R * 255),
                    (byte)(n.Foreground.Value.G * 255),
                    (byte)(n.Foreground.Value.B * 255)
                )
            );
        }

        Inline ConvertTree(TextTree n)
        {
            if (n is TextNode)
            {
                var tn = n as TextNode;

                Inline i = new Windows.UI.Xaml.Documents.Run { Text = tn.Text };
                ApplyStyles(n,i);
                if (tn.Underline == true)
                {
                    var u = new Windows.UI.Xaml.Documents.Underline();
                    u.Inlines.Add(i);
                    i = u;
                }
                return i;
            }
            else if (n is Controls.Span)
            {
                var sn = n as Controls.Span;

                Windows.UI.Xaml.Documents.Span s = sn.Underline == true ? new Windows.UI.Xaml.Documents.Underline() : new Windows.UI.Xaml.Documents.Span();
                ApplyStyles(n,s);
                foreach (var t in sn.Nodes) s.Inlines.Add(ConvertTree(t));
                return s;
            }

            return null;
        }
    }
}
