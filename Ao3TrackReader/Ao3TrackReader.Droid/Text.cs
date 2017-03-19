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

namespace Ao3TrackReader.Text
{
    public abstract partial class TextEx
    {
        public SpannableString ConvertToSpannable(StateNode state)
        {
            var nodes = Flatten(state).TrimNewLines();

            var s = new SpannableString(string.Concat(nodes as IEnumerable<TextEx>));
            int start = 0;
            foreach (var n in nodes)
            {
                if (string.IsNullOrEmpty(n.Text)) continue;

                if (n.Bold != null || n.Italic != null || n.FontSize != null || n.Foreground != null)
                {
                    TypefaceStyle style = 0;
                    if (n.Bold == true && n.Italic == true)
                        style = TypefaceStyle.BoldItalic;
                    else if (n.Bold == true)
                        style = TypefaceStyle.Bold;
                    else if (n.Italic == true)
                        style = TypefaceStyle.Italic;
                    else
                        style = TypefaceStyle.Normal;


                    ColorStateList color = null;
                    if (n.Foreground != null) color = ColorStateList.ValueOf(new AColor(
                            (byte)(n.Foreground.Value.R * 255),
                            (byte)(n.Foreground.Value.G * 255),
                            (byte)(n.Foreground.Value.B * 255),
                            (byte)(n.Foreground.Value.A * 255)
                        ));

                    int fontSize = -1;
                    if (n.FontSize != null) fontSize = (int)n.FontSize.Value;

                    s.SetSpan(new TextAppearanceSpan(null, style, fontSize, color, null), start, start + n.Text.Length, SpanTypes.InclusiveExclusive);
                }
                if (n.Sub == true)
                    s.SetSpan(new SubscriptSpan(), start, start + n.Text.Length, SpanTypes.InclusiveExclusive);
                if (n.Super == true)
                    s.SetSpan(new SuperscriptSpan(), start, start + n.Text.Length, SpanTypes.InclusiveExclusive);
                if (n.Strike == true)
                    s.SetSpan(new StrikethroughSpan(), start, start + n.Text.Length, SpanTypes.InclusiveExclusive);
                if (n.Underline == true)
                    s.SetSpan(new UnderlineSpan(), start, start + n.Text.Length, SpanTypes.InclusiveExclusive);

                start += n.Text.Length;
            }

            return s;
        }

    }
}
