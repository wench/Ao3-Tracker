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

            bool endblock = false;
            var inlines = ConvertTree(view.TextTree, ref endblock);
            TrimNewLines(inlines);

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
            if (n.Strike == true)
            {
                if (i.Foreground == null)
                {
                    i.Foreground = new SolidColorBrush();
                }
                i.Foreground.Opacity = 0.25;
            }
        }

        bool TrimNewLines(Inline inline, bool had = true, bool strip_all = true)
        {
            if (inline is Windows.UI.Xaml.Documents.LineBreak)
            {
                had = true;
            }
            else if (inline is Windows.UI.Xaml.Documents.Span)
            {
                var s = inline as Windows.UI.Xaml.Documents.Span;

                if (strip_all)
                {
                    while (s.Inlines.LastOrDefault() is Windows.UI.Xaml.Documents.LineBreak)
                        s.Inlines.RemoveAt(s.Inlines.Count - 1);
                }

                foreach (var c in s.Inlines.Reverse())
                {
                    had = TrimNewLines(c, had, strip_all);
                    strip_all = false;
                }
            }
            else if (inline is Windows.UI.Xaml.Documents.Run)
            {
                var r = inline as Windows.UI.Xaml.Documents.Run;
                if (had && r.Text.EndsWith("\n"))
                    r.Text = r.Text.TrimEnd('\n');
                had = r.Text.StartsWith("\n");
            }
            return had;
        }

        Inline ConvertTree(TextTree n, ref bool endblock)
        {
            if (n is TextNode)
            {
                var tn = n as TextNode;

                Inline i = new Windows.UI.Xaml.Documents.Run { Text = tn.Text };
                ApplyStyles(n, i);
                if (tn.Underline == true)
                {
                    var u = new Windows.UI.Xaml.Documents.Underline();
                    u.Inlines.Add(i);
                    i = u;
                }
                endblock = false;
                return i;
            }
            else if (n is Models.Block || n is Models.Span)
            {
                var sn = n as Models.Span;

                Windows.UI.Xaml.Documents.Span s = sn.Underline == true ? new Windows.UI.Xaml.Documents.Underline() : new Windows.UI.Xaml.Documents.Span();
                ApplyStyles(n, s);

                bool hadblock = false;
                foreach (var t in sn.Nodes)
                {
                    s.Inlines.Add(ConvertTree(t, ref hadblock));
                    if (t is Models.Block && hadblock == false)
                    {
                        s.Inlines.Add(new LineBreak());
                        s.Inlines.Add(new LineBreak());
                        hadblock = true;
                    }
                }

                endblock = hadblock;
                return s;
            }

            endblock = false;
            return null;
        }
    }
}
