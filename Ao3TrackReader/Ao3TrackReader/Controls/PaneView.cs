using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
	public class PaneView : ContentView
	{
        double old_width;
        public PaneView()
		{
            TranslationX = old_width = 480;
            WidthRequest = old_width;
        }

        public bool IsOnScreen
        {
            get
            {
                return TranslationX < Width / 2;
            }
            set
            {
                if (value == false)
                {
                    ViewExtensions.CancelAnimations(this);
                    this.TranslateTo(Width, 0, 100, Easing.CubicIn);
                }
                else
                {
                    ViewExtensions.CancelAnimations(this);
                    this.TranslateTo(0, 0, 100, Easing.CubicIn);
                }
                OnIsOnScreenChanging(value);
            }
        }

        protected virtual void OnIsOnScreenChanging(bool newValue)
        {

        }

        protected override void OnSizeAllocated(double width, double height)
        {
            if (width > 0)
            {
                bool wasshowing = TranslationX < old_width / 2;
                old_width = width;

                base.OnSizeAllocated(width, height);

                ViewExtensions.CancelAnimations(this);
                if (wasshowing) TranslationX = 0.0;
                else TranslationX = width;
            }
            else
            {
                base.OnSizeAllocated(width, height);
            }
        }

    }
}
