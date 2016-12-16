using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{   
    [Xamarin.Forms.ContentProperty("Children")]
    public class PaneContainer : AbsoluteLayout, IViewContainer<PaneView>
    {

        public new IList<PaneView> Children
        {
            get
            {
                return new ListConverter<PaneView, View>(base.Children);
            }
        }

        public PaneContainer()
		{
		}

        protected double PaneWidth(double width)
        {
            if (Width <= 0)
                return 480;
            else if (Width < 480)
                return Width;
            else if (Width < 960)
                return 480;
            else
                return Width /2;
        }

        protected override void OnSizeAllocated(Double width, Double height)
        {
            var paneWidth = PaneWidth(width);
            foreach (var child in Children) {
                AbsoluteLayout.SetLayoutBounds(child, new Rectangle(1, 0, paneWidth, 1));
            }
            base.OnSizeAllocated(width, height);
        }

        protected override void OnChildAdded(Element child)
        {
            AbsoluteLayout.SetLayoutBounds(child, new Rectangle(1, 0, PaneWidth(Width), 1));
            AbsoluteLayout.SetLayoutFlags(child, AbsoluteLayoutFlags.HeightProportional | AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.YProportional);
            base.OnChildAdded(child);
        }
    }
}
