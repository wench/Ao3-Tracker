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

	public class TextView : Xamarin.Forms.Label
	{
		public static readonly Xamarin.Forms.BindableProperty TextTreeProperty =
		  Xamarin.Forms.BindableProperty.Create("TextTree", typeof(Models.TextTree), typeof(TextView), defaultValue: null);

		public Models.TextTree TextTree
		{
			get { return (Models.TextTree)GetValue(TextTreeProperty); }
			set { SetValue(TextTreeProperty, value); }
		}
	}
}
