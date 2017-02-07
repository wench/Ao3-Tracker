﻿using System;
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
        static bool isTextDecorationsAvailable = Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Documents.TextElement", "TextDecorations");

        protected Inline ApplyStyles(Inline i)
        {
            if (isTextDecorationsAvailable)
            {
                i.TextDecorations = TextDecorations.None;
                if (Underline == true)
                {
                    i.TextDecorations = i.TextDecorations | TextDecorations.Underline;
                }
                if (Strike == true)
                {
                    i.TextDecorations = i.TextDecorations | TextDecorations.Strikethrough;
                }
            }
            else
            {
                if (Underline == true && !(i is Underline))
                {
                    var u = new Underline();
                    var s = i as Windows.UI.Xaml.Documents.Span;
                    if (s != null)
                    {
                        foreach (var e in s.Inlines) u.Inlines.Add(s);
                        s.Inlines.Clear();
                        return ApplyStyles(u);
                    }
                    u.Inlines.Add(i);
                    i = u;
                }
                if (Strike == true)
                {
                    i.CharacterSpacing = -250;
                }
            }

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
            var s = new Windows.UI.Xaml.Documents.Span();

            foreach (var n in Nodes)
            {
                s.Inlines.Add(n.ConvertToInline());
            }

            return ApplyStyles(s);
        }
    }

    public partial class Block
    {
        public override Inline ConvertToInline()
        {
            var s = new Windows.UI.Xaml.Documents.Span();

            foreach (var n in Nodes)
            {
                s.Inlines.Add(n.ConvertToInline());
            }

            if (Nodes.Count > 0 && Nodes[Nodes.Count - 1].GetType() != typeof(Block))
            {
                s.Inlines.Add(new Run { Text = "\n\n" });
            }
            return ApplyStyles(s);
        }
    }

}
