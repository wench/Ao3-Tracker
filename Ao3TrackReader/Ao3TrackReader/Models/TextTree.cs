using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Models
{
    public abstract class TextTree
    {
        public double? FontSize { get; set; }
        public bool? Bold { get; set; }
        public bool? Italic { get; set; }
        public bool? Underline { get; set; }
        public bool? Strike { get; set; }
        public bool? Sub { get; set; }
        public bool? Super { get; set; }
        public Xamarin.Forms.Color? Foreground { get; set; }

        public static implicit operator TextTree(string str)
        {
            return new TextNode { Text = str };
        }

        public abstract override string ToString();

    }

    public class TextNode : TextTree
    {
        public TextNode()
        {
        }
        public string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }

    public class Span : TextTree
    {
        public Span()
        {
            Nodes = new List<TextTree>();
        }

        public IList<TextTree> Nodes { get; private set; }

        public override string ToString()
        {
            return string.Join("", Nodes);
        }
    }

    public class Block : Span
    {
        public override string ToString()
        {
            return base.ToString().TrimEnd('\n') + "\n\n";
        }
    }


}
