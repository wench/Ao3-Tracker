using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
    public class TabView : ContentView
    {
        public static readonly BindableProperty TitleProperty = BindableProperty.Create("Title", typeof(string), typeof(TabView), null);
        public static readonly BindableProperty IconProperty = BindableProperty.Create("Icon", typeof(FileImageSource), typeof(TabView), null);

        public new static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(ContentView), defaultValue: null,
            propertyChanged: (b, o, n) => (b as TabView).OnContentOrScrollChanged());

        public static readonly BindableProperty ScrollProperty = BindableProperty.Create(nameof(Scroll), typeof(bool), typeof(ContentView), defaultValue: true,
            propertyChanged: (b, o, n) => (b as TabView).OnContentOrScrollChanged());

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public FileImageSource Icon
        {
            get => (FileImageSource)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }


        public new View Content
        {
            get => (View)GetValue(ContentProperty); 
            set => SetValue(ContentProperty, value); 
        }

        public bool Scroll
        {
            get => (bool)GetValue(ScrollProperty); 
            set => SetValue(ScrollProperty, value); 
        }

        public TabView() 
        {
        }

        void OnContentOrScrollChanged()
        {
            if (Scroll)
            {
                Padding = new Thickness(0);
                base.Content = new ScrollView { Orientation = ScrollOrientation.Vertical, Padding = new Thickness(0, 0, 0, 16.0), Content = Content };
            }
            else
            {
                Padding = new Thickness(0, 0, 0, 16.0);
                base.Content = Content;
            }
        }
    }
}
