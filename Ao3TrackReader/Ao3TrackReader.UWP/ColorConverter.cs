using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Ao3TrackReader.UWP
{
    class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var color = (Xamarin.Forms.Color) value;
            if (color == null || color == Xamarin.Forms.Color.Default) return null;
            return new SolidColorBrush(color.ToWindows());
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var brush = (SolidColorBrush)value;
            if (brush == null) return Xamarin.Forms.Color.Default;
            else return brush.Color.ToXamarin();
        }
    }
}
