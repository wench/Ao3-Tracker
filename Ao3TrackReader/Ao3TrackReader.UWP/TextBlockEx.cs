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
        public static readonly DependencyProperty InlinesExProperty =
            DependencyProperty.RegisterAttached("InlinesEx", typeof(IList<Windows.UI.Xaml.Documents.Inline>), typeof(TextBlockEx), new PropertyMetadata(null, InlinesExChanged));

        private static void InlinesExChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textblock = d as TextBlock;
            var value = e.NewValue as IList<Windows.UI.Xaml.Documents.Inline>;
            if (textblock != null)
            {
                textblock.Inlines.Clear();
                if (value != null) foreach (var i in value)
                    {
                        textblock.Inlines.Add(i);
                    }
            }
        }
        public static void SetInlinesEx(UIElement element, IList<Windows.UI.Xaml.Documents.Inline> value)
        {
            element.SetValue(InlinesExProperty, value);

        }
        public static IList<Windows.UI.Xaml.Documents.Inline> GetInlinesEx(UIElement element)
        {
            return (IList <Windows.UI.Xaml.Documents.Inline>) element.GetValue(InlinesExProperty);
        }
    }
}
