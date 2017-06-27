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
using Windows.UI.Text;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Xaml.Documents;

namespace Ao3TrackReader.Text
{
    public abstract partial class TextEx
    {
        protected Inline ApplyStyles(Inline i)
        {
#if WINDOWS_15063 || WINDOWS_FUTURE
            if (Ao3TrackReader.UWP.App.UniversalApi >= 4)
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
#endif
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

        public static implicit operator Inline(TextEx tree)
        {
            return tree.ConvertToInline();
        }
    }

    public partial class String
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

            bool donefirst = false;
            foreach (var n in Nodes)
            {
                if (Pad && donefirst) s.Inlines.Add(new Windows.UI.Xaml.Documents.Run { Text = " " });
                s.Inlines.Add(n.ConvertToInline());
                donefirst = true;
            }

            return ApplyStyles(s);
        }
    }

    public partial class A
    {
        public override Inline ConvertToInline()
        {
            var s = new Windows.UI.Xaml.Documents.Hyperlink();
            s.Click += (sender, args) =>
            {
                OnClick();
            };

            bool donefirst = false;
            foreach (var n in Nodes)
            {
                if (Pad && donefirst) s.Inlines.Add(new Windows.UI.Xaml.Documents.Run { Text = " " });
                s.Inlines.Add(n.ConvertToInline());
                donefirst = true;
            }

#if WINDOWS_UWP
            return ApplyStyles(s);
#else
            var u = new Underline();
            u.Inlines.Add(s);
            return ApplyStyles(u);
#endif
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
