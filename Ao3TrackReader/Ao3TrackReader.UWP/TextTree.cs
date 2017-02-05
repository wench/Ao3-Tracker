using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Xaml.Documents;

namespace Ao3TrackReader.Models
{
    public abstract partial class TextTree
    {
        protected T ApplyStyles<T>(T i)
            where T: Inline
        {
            if (Bold == true) i.FontWeight = FontWeights.Bold;
            else if (Bold == false) i.FontWeight = FontWeights.Normal;

            if (Italic == true) i.FontStyle = FontStyle.Italic;
            else if (Italic == false) i.FontStyle = FontStyle.Normal;

            if (FontSize != null) i.FontSize = (double)FontSize;

            if (Foreground != null) i.Foreground = new SolidColorBrush(
                Color.FromArgb(
                    (byte)(Foreground.Value.A * 255),
                    (byte)(Foreground.Value.R * 255),
                    (byte)(Foreground.Value.G * 255),
                    (byte)(Foreground.Value.B * 255)
                )
            );
            i.TextDecorations = TextDecorations.None;
            if (Strike == true)
            {
                i.TextDecorations = i.TextDecorations | TextDecorations.Strikethrough;
            }
            if (Underline == true)
            {
                i.TextDecorations = i.TextDecorations | TextDecorations.Underline;
            }

            return i;
        }

        public virtual Inline ConvertToInline()
        {
            return new Run();
        }

        public static implicit operator Inline(TextTree tree)
        {
            return tree.ConvertToInline();
        }
    }

    public partial class TextNode
    {
        public override Inline ConvertToInline()
        {
            return ApplyStyles(new Windows.UI.Xaml.Documents.Run { Text = Text });
        }
    }

    public partial class Span
    {
        public override Inline ConvertToInline()
        {
            Windows.UI.Xaml.Documents.Span s = ApplyStyles(new Windows.UI.Xaml.Documents.Span());

            foreach (var n in Nodes)
            {
                s.Inlines.Add(n.ConvertToInline());
            }

            return s;
        }
    }

    public partial class Block
    {
        public override Inline ConvertToInline()
        {
            Windows.UI.Xaml.Documents.Span s = ApplyStyles(new Windows.UI.Xaml.Documents.Span());

            foreach (var n in Nodes)
            {
                s.Inlines.Add(n.ConvertToInline());
            }

            if (Nodes.Count > 0 && Nodes[Nodes.Count - 1].GetType() != typeof(Block))
            {
                s.Inlines.Add(new Run { Text = "\n\n" });
            }
            return s;
        }
    }

}
