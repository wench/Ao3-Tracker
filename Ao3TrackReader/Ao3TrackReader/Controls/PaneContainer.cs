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
        public new IList<PaneView> Children => new ListConverter<PaneView, View>(base.Children);

        public PaneContainer()
		{
            InputTransparent = true;
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
            if (width > 0) RecalculateVisbility();
        }

        protected override void OnChildAdded(Element child)
        {
            AbsoluteLayout.SetLayoutBounds(child, new Rectangle(1, 0, PaneWidth(Width), 1));
            AbsoluteLayout.SetLayoutFlags(child, AbsoluteLayoutFlags.HeightProportional | AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.YProportional);
            base.OnChildAdded(child);

            var c = child as PaneView;
            c.PropertyChanged += Child_PropertyChanged;
            c.IsOnScreenChanged += Child_IsOnScreenChanged;
        }

        private void Child_IsOnScreenChanged(object sender, EventArgs<bool> args)
        {
            RecalculateVisbility();
        }

        protected override void OnChildRemoved(Element child)
        {
            var c = child as PaneView;
            c.PropertyChanged -= Child_PropertyChanged;
            c.IsOnScreenChanged -= Child_IsOnScreenChanged;
        }

        private void Child_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "IsVisible")
            {
                RecalculateVisbility();
            }
        }

        void RecalculateVisbility()
        {
            bool visible = false;
            foreach (var child in Children) visible |= child.IsVisible;

            InputTransparent = !visible;
        }
    }
}
