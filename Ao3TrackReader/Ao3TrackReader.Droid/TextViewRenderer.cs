using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Label = Xamarin.Forms.Label;
using Xamarin.Forms.Platform.Android;
using Ao3TrackReader.Controls;
using Ao3TrackReader.Models;
using Android.Content.Res;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Widget;
using AColor = Android.Graphics.Color;
using TextView = Ao3TrackReader.Controls.TextView;
using Android.Runtime;
using Android.Text.Style;

[assembly: Xamarin.Forms.ExportRenderer(typeof(TextView), typeof(Ao3TrackReader.Droid.TextViewRenderer))]
namespace Ao3TrackReader.Droid
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

            if (view != null && (e.PropertyName == Label.TextColorProperty.PropertyName || 
                e.PropertyName == Label.FontProperty.PropertyName ||
                e.PropertyName == Label.TextProperty.PropertyName ||
                e.PropertyName == Label.FormattedTextProperty.PropertyName ||
                e.PropertyName == TextView.TextTreeProperty.PropertyName))
            {
                if (view.TextTree != null)
                {
                    if (view.FormattedText == null)
                    {
                        view.FormattedText = "";
                        return;
                    }
                    base.OnElementPropertyChanged(sender, e);
                    UpdateControl(view);
                    return;
                }
            }

            base.OnElementPropertyChanged(sender, e);
        }
        
        void UpdateControl(TextView view)
        {
            if (view.TextTree == null) return;

            Control.TextFormatted = Convert(view.TextTree.Flatten(new StateNode { Foreground = view.TextColor }).TrimNewLines());
        }

        SpannableString Convert(ICollection<TextNode> nodes)
        {
            var s = new SpannableString(string.Concat(nodes as IEnumerable<TextNode>));
            int start = 0;
            foreach (var n in nodes)
            {
                if (string.IsNullOrEmpty(n.Text)) continue;

                if (n.Bold != null || n.Italic != null || n.FontSize != null || n.Foreground != null)
                {
                    TypefaceStyle style = 0;
                    if (n.Bold == true && n.Italic == true)
                        style = TypefaceStyle.BoldItalic;
                    else if (n.Bold == true)
                        style = TypefaceStyle.Bold;
                    else if (n.Italic == true)
                        style = TypefaceStyle.Italic;
                    else
                        style = TypefaceStyle.Normal;


                    ColorStateList color = null;
                    if (n.Foreground != null) color = ColorStateList.ValueOf(new AColor(
                            (byte)(n.Foreground.Value.R * 255),
                            (byte)(n.Foreground.Value.G * 255),
                            (byte)(n.Foreground.Value.B * 255),
                            (byte)(n.Foreground.Value.A * 255)
                        ));

                    int fontSize = -1;
                    if (n.FontSize != null) fontSize = (int)n.FontSize.Value;

                    s.SetSpan(new TextAppearanceSpan(null, style, fontSize, color, null), start, start + n.Text.Length, SpanTypes.InclusiveExclusive);
                }
                if (n.Sub == true)
                    s.SetSpan(new SubscriptSpan(), start, start + n.Text.Length, SpanTypes.InclusiveExclusive);
                if (n.Super == true)
                    s.SetSpan(new SuperscriptSpan(), start, start + n.Text.Length, SpanTypes.InclusiveExclusive);
                if (n.Strike == true)
                    s.SetSpan(new StrikethroughSpan(), start, start + n.Text.Length, SpanTypes.InclusiveExclusive);
                if (n.Underline == true)
                    s.SetSpan(new UnderlineSpan(), start, start + n.Text.Length, SpanTypes.InclusiveExclusive);

                start += n.Text.Length;
            }

            return s;
        }
    }
}
