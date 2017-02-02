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
            Control.Margin = new Windows.UI.Xaml.Thickness(0.0, 0.0, 12.0, 0.0);
            var flat = view.TextTree.Flatten(new StateNode());
            flat = flat.TrimNewLines();
            var inlines = Convert(flat);
            Control.Inlines.Add(inlines);
            Control.InvalidateArrange();
            Control.InvalidateMeasure();
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
            i.TextDecorations = TextDecorations.None;
            if (n.Underline == true)
            {
                i.TextDecorations = i.TextDecorations | TextDecorations.Underline;
            }
            if (n.Strike == true)
            {
                i.TextDecorations = i.TextDecorations | TextDecorations.Strikethrough;
            }
        }

        Inline Convert(ICollection<TextNode> n)
        {
            if (n.Count == 1)
            {
                var tn = n.First();

                Inline i = new Windows.UI.Xaml.Documents.Run { Text = tn.Text };
                ApplyStyles(tn, i);
                return i;
            }
            else 
            {
                Windows.UI.Xaml.Documents.Span s = new Windows.UI.Xaml.Documents.Span();

                foreach (var tn in n)
                {
                    Inline i = new Windows.UI.Xaml.Documents.Run { Text = tn.Text };
                    ApplyStyles(tn, i);
                    s.Inlines.Add(i);
                }

                return s;
            }
        }
    }
}
