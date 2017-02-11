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
    class NavigationPage : Xamarin.Forms.NavigationPage, IPageEx
    {
        public NavigationPage(WebViewPage page) : base(page)
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

        public Models.TextTree TitleEx
        {
            get
            {
                return PageEx.GetTitleEx(this);
            }
        }
    }
}
