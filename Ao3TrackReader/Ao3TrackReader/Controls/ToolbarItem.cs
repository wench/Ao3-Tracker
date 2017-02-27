using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
    public class ToolbarItem : Xamarin.Forms.ToolbarItem
    {
        public static readonly BindableProperty ForegroundProperty =
          BindableProperty.Create("Foreground", typeof(Color), typeof(ToolbarItem), defaultValue: Color.Default);

        public Color Foreground
        {
            get { return (Color)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }
    }
}
