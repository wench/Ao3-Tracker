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
