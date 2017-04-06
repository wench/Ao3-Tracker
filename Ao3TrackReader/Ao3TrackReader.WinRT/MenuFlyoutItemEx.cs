using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Ao3TrackReader.WinRT
{
    public class MenuFlyoutItemEx : MenuFlyoutItem
    {
        public MenuFlyoutItemEx()
        {

        }
        static DependencyProperty GetIconDependencyProperty ()
        {
#if WINDOWS_UWP
            if (Ao3TrackReader.UWP.App.UniversalApi >= 4)
                return MenuFlyoutItem.IconProperty;
#endif
            return DependencyProperty.Register(
                "Icon",
                typeof(IconElement),
                typeof(MenuFlyoutItemEx),
                new PropertyMetadata(null)
            );
        }

#if WINDOWS_15063
        new
#endif
        public static readonly DependencyProperty IconProperty = GetIconDependencyProperty();

#if WINDOWS_15063
        new 
#endif
        public IconElement Icon
        {
            get { return (IconElement)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

    }
}
