using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
	public class ScrollView : Xamarin.Forms.ScrollView
    {
        public event EventHandler ScrollEnd;

        internal void SetScrollEnd()
        {
            ScrollEnd?.Invoke(this, EventArgs.Empty);
        }
	}
}