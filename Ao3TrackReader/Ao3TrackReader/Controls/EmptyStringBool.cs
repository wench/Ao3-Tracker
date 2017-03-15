﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
    public class EmptyStringBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.GetType() == typeof(bool)) return value;

            var ifempty = false;

            if (parameter?.GetType() == typeof(bool)) ifempty = (bool)parameter;

            if (value == null) return ifempty;

            var s = value.ToString();

            if (!string.IsNullOrEmpty(s)) return !ifempty;
            return ifempty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
