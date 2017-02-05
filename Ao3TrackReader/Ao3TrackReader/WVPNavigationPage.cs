using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Ao3TrackReader.Models;
using Xamarin.Forms;
#if WINDOWS_UWP
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.WindowsSpecific;
#endif

namespace Ao3TrackReader
{
    class WVPNavigationPage : NavigationPage
    {
        public static readonly BindableProperty TitleExProperty =
          BindableProperty.CreateAttached("TitleEx", typeof(Models.TextTree), typeof(WVPNavigationPage), null, propertyChanged: TitleExPropertChanged);

        public WVPNavigationPage(WebViewPage page) : base(page)
        {
#if WINDOWS_UWP
            On<Xamarin.Forms.PlatformConfiguration.Windows>().SetToolbarPlacement(ToolbarPlacement.Bottom);
#endif
            BarBackgroundColor = Ao3TrackReader.Resources.Colors.Alt.Solid.MediumHigh;
        }

        protected override bool OnBackButtonPressed()
        {
            if (CurrentPage.SendBackButtonPressed())
                return true;

            return base.OnBackButtonPressed();
        }

        private static void TitleExPropertChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var val = newValue as Models.TextTree;
            var page = bindable as Xamarin.Forms.Page;

            if (page != null)
            {
                page.Title = val != null ? val.ToString() : null;
            }
        }

        public static Models.TextTree GetTitleEx(BindableObject view)
        {
            return (Models.TextTree)view.GetValue(TitleExProperty);
        }

        public static void SetTitleEx(BindableObject view, Models.TextTree value)
        {
            view.SetValue(TitleExProperty, value);
        }
        public Models.TextTree TitleEx
        {
            get
            {
                return WVPNavigationPage.GetTitleEx(this);
            }
        }
    }
}
