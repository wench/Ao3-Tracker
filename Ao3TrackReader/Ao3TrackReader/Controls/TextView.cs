using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

using LineBreakMode = Xamarin.Forms.LineBreakMode;
using BindableObject = Xamarin.Forms.BindableObject;

#if WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;
using NativeTextControl = Windows.UI.Xaml.Controls.TextBlock;
using Native = Windows.UI.Xaml.Documents.Inline;
using SolidColorBrush = Windows.UI.Xaml.Media.SolidColorBrush;
using Windows.UI;
using Windows.UI.Text;
#endif

namespace Ao3TrackReader.Controls
{
	public abstract class TextTree 
	{
		public double? FontSize { get; set; }
		public bool? Bold { get; set; }
		public bool? Italic { get; set; }
		public bool? Underline { get; set; }
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
			return string.Join("",Nodes);
		}
	}


	public class TextView : Xamarin.Forms.Label
	{
		public static readonly Xamarin.Forms.BindableProperty TextTreeProperty =
		  Xamarin.Forms.BindableProperty.Create("TextTree", typeof(TextTree), typeof(TextView), defaultValue: null);

		public TextTree TextTree
        {
			get { return (TextTree)GetValue(TextTreeProperty); }
			set { SetValue(TextTreeProperty, value); }
		}
	}
}
