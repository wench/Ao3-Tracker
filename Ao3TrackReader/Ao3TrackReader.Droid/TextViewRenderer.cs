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
using System.Text;
using System.Threading.Tasks;
using Label = Xamarin.Forms.Label;
using Xamarin.Forms.Platform.Android;
using Ao3TrackReader.Controls;
using Ao3TrackReader.Models;
using Android.Content.Res;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Widget;
using AColor = Android.Graphics.Color;
using TextView = Ao3TrackReader.Controls.TextView;
using Android.Runtime;
using Android.Text.Style;

[assembly: Xamarin.Forms.ExportRenderer(typeof(TextView), typeof(Ao3TrackReader.Droid.TextViewRenderer))]
namespace Ao3TrackReader.Droid
{
    class TextViewRenderer : LabelRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            base.OnElementChanged(e);

            var view = e.NewElement as TextView;
            if (view != null)
            {
                UpdateControl(view);
            }
        }

        protected override void OnElementPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var view = Element as TextView;

            if (view != null && (e.PropertyName == Label.TextColorProperty.PropertyName ||
                e.PropertyName == Label.FontProperty.PropertyName ||
                e.PropertyName == Label.TextProperty.PropertyName ||
                e.PropertyName == Label.FormattedTextProperty.PropertyName))
            {
                if (view.TextEx != null)
                {
                    base.OnElementPropertyChanged(sender, e);
                    UpdateControl(view);
                    return;
                }
            }
            else if (e.PropertyName == TextView.TextExProperty.PropertyName)
            {
                if (view.TextEx != null)
                {
                    var tts = view.TextEx.ToString();
                    view.FormattedText = tts;
                }

                base.OnElementPropertyChanged(sender, e);
            }
        }
        
        void UpdateControl(TextView view)
        {
            if (view.TextEx == null) return;
            Control.TextFormatted = view.TextEx.ConvertToSpannable(new Text.StateNode { Foreground = view.TextColor });
        }

    }
}
