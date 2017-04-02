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
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Ao3TrackReader
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
