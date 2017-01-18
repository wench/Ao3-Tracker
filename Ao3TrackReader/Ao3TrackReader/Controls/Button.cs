using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
    class Button : Xamarin.Forms.Button
    {
        public static readonly Xamarin.Forms.BindableProperty ImageHeightProperty =
          Xamarin.Forms.BindableProperty.Create("ImageHeight", typeof(double), typeof(Button), defaultValue: 0.0);
        public static readonly Xamarin.Forms.BindableProperty ImageWidthProperty =
          Xamarin.Forms.BindableProperty.Create("ImageWidth", typeof(double), typeof(Button), defaultValue: 0.0);

        public double ImageHeight
        {
            get { return (double)GetValue(ImageHeightProperty); }
            set { SetValue(ImageHeightProperty, value); }
        }
        public double ImageWidth
        {
            get { return (double)GetValue(ImageWidthProperty); }
            set { SetValue(ImageWidthProperty, value); }
        }

    }
}
