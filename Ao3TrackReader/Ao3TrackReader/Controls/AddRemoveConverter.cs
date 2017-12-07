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
using System.Globalization;
using System.Text;
using Ao3TrackReader.Models;
using Xamarin.Forms;


namespace Ao3TrackReader.Controls
{
    class AddRemoveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = false;
            if (value is bool) b = (bool)value;
            else if (value != null) b = string.Compare(value.ToString(), bool.TrueString, true) == 0;

            if (b)
                return parameter is null ? "Remove": "Remove from " + parameter.ToString();
            else
                return parameter is null ? "Add" : "Add to " + parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                if (parameter == null) return s == "Remove";
                else return s == ("Remove from " + parameter.ToString());
            }

            return false;
        }

        public static AddRemoveConverter Instance { get; } = new AddRemoveConverter();
    }
}
