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
            BackgroundColor = Ao3TrackReader.Resources.Colors.Alt.Trans.High;
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
                    this.AbortAnimation("OffOnAnim");
                    if (TranslationX != Width)
                    {
                        this.Animate("OffOnAnim", new Animation((f) => TranslationX = f, 0, Width, Easing.CubicIn), 16, 100, finished: (f, cancelled) =>
                        {
                            if (!cancelled) IsVisible = false;
                        });
                    }
                    else
                    {
                        IsVisible = false;
                    }
                }
                else
                {
                    this.AbortAnimation("OffOnAnim");
                    IsVisible = true;
                    if (TranslationX != 0)
                    {
                        this.Animate("OffOnAnim", new Animation((f) => TranslationX = f, TranslationX, 0, Easing.CubicIn), 16, 100, finished: (f, cancelled) =>
                        {
                            if (!cancelled) IsVisible = true;
                        });
                    }
                    else
                    {
                        IsVisible = true;
                    }
                }
                OnIsOnScreenChanging(value);
                IsOnScreenChanged?.Invoke(this,value);
            }
        }
        public event EventHandler<bool> IsOnScreenChanged;

        protected virtual void OnIsOnScreenChanging(bool newValue)
        {

        }

        protected override void OnSizeAllocated(double width, double height)
        {
            if (width > 0)
            {
                bool wasshowing = TranslationX < old_width / 2;
                if (old_width != width) this.AbortAnimation("OffOnAnim");
                old_width = width;

                base.OnSizeAllocated(width, height);

                if (wasshowing)
                {
                    TranslationX = 0.0;
                    IsVisible = true;
                }
                else
                {
                    TranslationX = width;
                    IsVisible = this.AnimationIsRunning("OffOnAnim");
                }
            }
            else
            {
                base.OnSizeAllocated(width, height);
            }
        }

    }
}
