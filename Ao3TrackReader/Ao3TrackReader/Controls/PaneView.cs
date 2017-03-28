/*
Copyright 2017 Alexis Ryan

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

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

        private WebViewPage _wvp;
        public WebViewPage wvp { get { return _wvp; }
            set { _wvp = value; OnWebViewPageSet(); }
        }

        public PaneView()
        {
            TranslationX = old_width = 480;
            WidthRequest = old_width;
            BackgroundColor = Ao3TrackReader.Resources.Colors.Alt.Trans.VeryHigh;
            //IsVisible = false;
        }

        protected virtual void OnClose(object sender, EventArgs e)
        {
            IsOnScreen = false;
        }

        protected virtual void OnWebViewPageSet()
        {

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
                IsOnScreenChanged?.Invoke(this,new EventArgs<bool>(value));
            }
        }
        public event EventHandler<EventArgs<bool>> IsOnScreenChanged;

        protected virtual void OnIsOnScreenChanging(bool newValue)
        {

        }

        protected override void OnSizeAllocated(double width, double height)
        {
            if (width > 0)
            {
                bool wasshowing = IsVisible && TranslationX < old_width / 2;
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
