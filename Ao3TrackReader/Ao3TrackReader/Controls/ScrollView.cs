using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
	public class ScrollView : Xamarin.Forms.ScrollView
    {
        public static readonly BindableProperty HideScrollbarsProperty = BindableProperty.Create("HideScrollbars", typeof(bool), typeof(ScrollView), false);

        public event EventHandler ScrollEnd;

        internal void SetScrollEnd()
        {
            ScrollEnd?.Invoke(this, EventArgs.Empty);
        }

        public bool HideScrollbars
        {
            get
            {
                return (bool)GetValue(HideScrollbarsProperty);
            }
            set
            {
                SetValue(HideScrollbarsProperty, value);
            }
        }        
    }
}