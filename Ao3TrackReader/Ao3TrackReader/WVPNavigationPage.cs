using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Ao3TrackReader
{
    class WVPNavigationPage : NavigationPage
    {
        public WVPNavigationPage(WebViewPage page) : base(page)
        {

        }

        protected override bool OnBackButtonPressed()
        {
            if (CurrentPage.SendBackButtonPressed())
                return true;

            return base.OnBackButtonPressed();
        }
    }
}
