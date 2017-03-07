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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Ao3TrackReader.UWP
{
    class TextBlockEx : DependencyObject
    {
        public static readonly DependencyProperty TextExProperty =
            DependencyProperty.RegisterAttached("TextEx", typeof(Models.TextTree), typeof(TextBlockEx), new PropertyMetadata(null, TextExChanged));

        private static void TextExChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textblock = d as TextBlock;
            var value = e.NewValue as Models.TextTree;
            if (textblock != null)
            {
                textblock.Inlines.Clear();
                if (value != null)
                {
                    foreach (var i in value.Flatten(new Models.StateNode()))
                        textblock.Inlines.Add(i);
                }
            }
        }
        public static void SetTextEx(UIElement element, Models.TextTree value)
        {
            element.SetValue(TextExProperty, value);

        }
        public static Models.TextTree GetTextEx(UIElement element)
        {
            return (Models.TextTree) element.GetValue(TextExProperty);
        }
    }
}
