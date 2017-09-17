using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
    public class TabView : ScrollView
    {
        public static readonly BindableProperty TitleProperty = BindableProperty.Create("Title", typeof(string), typeof(TabView), null);
        public static readonly BindableProperty IconProperty = BindableProperty.Create("Icon", typeof(FileImageSource), typeof(TabView), null);

        public string Title
        {
            get
            {
                return (string)GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty, value);
            }
        }

        public FileImageSource Icon
        {
            get
            {
                return (FileImageSource)GetValue(IconProperty);
            }
            set
            {
                SetValue(IconProperty, value);
            }
        }

        public TabView() 
        {
            Orientation = ScrollOrientation.Vertical; 
        }
    }
}
