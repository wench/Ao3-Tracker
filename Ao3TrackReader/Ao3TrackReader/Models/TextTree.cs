using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ao3TrackReader.Models
{
    static public class Extensions
    {
        static public ICollection<TextNode> TrimNewLines(this ICollection<TextNode> col)
        {
            for (;;)
            {
                var node = col.LastOrDefault();
                if (node == null) break;
                node.Text = node.Text.TrimEnd('\n', '\r', '\t', ' ');
                if (node.Text.Length > 0) break;
                col.Remove(node);
            }
            return col;
        }
    }


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
        public void ApplyState(TextTree state)
        {
            if (FontSize == null) FontSize = state.FontSize;
            if (Bold == null) Bold = state.Bold;
            if (Italic == null) Italic = state.Italic;
            if (Strike == null) Strike = state.Strike;
            if (Sub == null) Sub = state.Sub;
            if (Super == null) Super = state.Super;
            if (Foreground == null) Foreground = state.Foreground;
        }

        public abstract override string ToString();

        public abstract ICollection<TextNode> Flatten(StateNode state);
    }

    public class StateNode : TextTree
    {
        public override ICollection<TextNode> Flatten(StateNode state)
        {
            return new TextNode[] { };
        }

        public override string ToString()
        {
            return "";
        }
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

        public override ICollection<TextNode> Flatten(StateNode state)
        {
            TextNode res = (TextNode) this.MemberwiseClone();
            res.ApplyState(state);
            return new[] { res };
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
        public override ICollection<TextNode> Flatten(StateNode state)
        {
            var newstate = new StateNode();
            newstate.ApplyState(this);
            newstate.ApplyState(state);

            List<TextNode> res = new List<TextNode>(Nodes.Count + 1);
            foreach (var node in Nodes)
            {
                res.AddRange(node.Flatten(newstate));
            }

            return res;
        }
    }

    public class Block : Span
    {
        public override string ToString()
        {
            bool lastisblock = Nodes.Count > 0 && Nodes[Nodes.Count - 1].GetType() == typeof(Block);
            return base.ToString() + (!lastisblock?"\n\n":"");
        }

        public override ICollection<TextNode> Flatten(StateNode state)
        {
            var res = base.Flatten(state) as List<TextNode>;
            if (Nodes.Count > 0 && Nodes[Nodes.Count - 1].GetType() != typeof(Block))
            {
                var linebreaks = new TextNode { Text = "\n\n" };
                linebreaks.ApplyState(this);
                linebreaks.ApplyState(state);
                res.Add(linebreaks);
            }
            return res;
        }

    }


}
