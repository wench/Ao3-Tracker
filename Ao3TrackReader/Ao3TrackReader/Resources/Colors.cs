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
using System.Text;
using Xamarin.Forms;

namespace Ao3TrackReader.Resources
{
    public class BaseColorSet
    {
        public Color High { get; protected set; }
        public Color MediumHigh { get; protected set; }
        public Color Medium { get; protected set; }
        public Color MediumLow { get; protected set; }
        public Color Low { get; protected set; }
        public Color VeryLow { get; protected set; }
        public Color VeryVeryLow { get; protected set; }
    }

    public class TransColorSet : BaseColorSet
    {
        public TransColorSet(Color col)
        {
            High = col.MultiplyAlpha(0.9);
            MediumHigh = col.MultiplyAlpha(0.8);
            Medium = col.MultiplyAlpha(0.6);
            MediumLow = col.MultiplyAlpha(0.4);
            Low = col.MultiplyAlpha(0.2);
            VeryLow = col.MultiplyAlpha(0.1);
            VeryVeryLow = col.MultiplyAlpha(0.05);
        }

    }


    public class ColorSet : BaseColorSet
    {
        public ColorSet(Color col,Color bg)
        {
            Trans = new TransColorSet(col);
            Solid = this;

            High = col;
            MediumHigh = Trans.High.Blend(bg);
            Medium = Trans.MediumHigh.Blend(bg);
            MediumLow = Trans.Medium.Blend(bg);
            Low = Trans.MediumLow.Blend(bg);
            VeryLow = Trans.Low.Blend(bg);
            VeryVeryLow = Trans.VeryLow.Blend(bg);

        }

        public static implicit operator Color(ColorSet set)
        {
            return set.High;
        }
        public BaseColorSet Solid { get; protected set; }
        public BaseColorSet Trans { get; protected set; }
    }

    public static class Colors
    {
        static Colors()
        {
            Color b, a, h, i;

            switch (App.Theme)
            {
                default:
                case "light":
                    b = Color.Black;
                    a = Color.White;
                    break;

                case "dark":
                    b = Color.White;
                    a = Color.Black;
                    break;
            }
            h = new Color(((int)(b.R * 255) ^ 165) / 255.0, ((int)(b.G * 255) ^ 0) / 430.0, ((int)(b.B * 255) ^ 0) / 300.0);
            i = new Color(((int)(a.R * 255) ^ 165) / 255.0, ((int)(a.G * 255) ^ 0) / 430.0, ((int)(a.B * 255) ^ 0) / 300.0);

            Base = new ColorSet(b, a);
            Alt = new ColorSet(a, b);
            Highlight = new ColorSet(h, a);
            Inverse = new ColorSet(i, b);
        }

        public static ColorSet Base { get; private set; }
        public static ColorSet Alt { get; private set; }

        public static ColorSet Highlight { get; private set; }
        public static ColorSet Inverse { get; private set; }
    }
}
