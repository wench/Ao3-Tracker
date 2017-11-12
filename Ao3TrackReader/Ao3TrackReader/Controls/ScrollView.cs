using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
	public class ScrollView : Xamarin.Forms.ScrollView
    {
        public static readonly BindableProperty LeaveSpaceProperty = BindableProperty.Create("LeaveSpace", typeof(double), typeof(ScrollView), 0.0);

        public event EventHandler ScrollEnd;

        internal void SetScrollEnd()
        {
            ScrollEnd?.Invoke(this, EventArgs.Empty);
        }

        public double LeaveSpace
        {
            get
            {
                return (double)GetValue(LeaveSpaceProperty);
            }
            set
            {
                SetValue(LeaveSpaceProperty, value);
            }
        }

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            if (LeaveSpace != 0.0)
            {
                if (!double.IsPositiveInfinity(widthConstraint))
                {
                    widthConstraint -= LeaveSpace;
                }
            }

            return base.OnMeasure(widthConstraint, heightConstraint);
        }
    }
}