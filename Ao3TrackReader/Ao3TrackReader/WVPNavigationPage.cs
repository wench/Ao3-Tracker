using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
#if WINDOWS_UWP
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.WindowsSpecific;
#endif

namespace Ao3TrackReader
{
    class WVPNavigationPage : NavigationPage
    {
        public WVPNavigationPage(WebViewPage page) : base(page)
        {
#if WINDOWS_UWP
            On<Xamarin.Forms.PlatformConfiguration.Windows>().SetToolbarPlacement(ToolbarPlacement.Bottom);
#endif
        }

        protected override bool OnBackButtonPressed()
        {
            if (CurrentPage.SendBackButtonPressed())
                return true;

            return base.OnBackButtonPressed();
        }
    }
}
