using Ao3TrackReader.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

using Xamarin.Forms;

namespace Ao3TrackReader
{
    public class App : Application
    {
        public static Ao3TrackDatabase Database
        {
            get; private set;
        }

        public static SyncedStorage Storage
        {
            get; private set;
        }


        static App()
        {
        }

        public static Dictionary<string, Color> Colors { get; private set; }

        public static bool IsDark { get { return true; } }

        public static string PlatformIcon(string name)
        {
            string dl = IsDark ? "_dark" : "_light";
            return Device.OnPlatform(name + dl, name + dl, "Assets/" + name + dl);
        }

        public App()
        {
            Database = new Ao3TrackDatabase();
            Storage = new SyncedStorage();

            Colors = new Dictionary<string, Color>();

            foreach (var r in new string[] { "SystemBaseHighColor", "SystemBaseMediumColor", "SystemBaseLowColor", "SystemBaseMediumHighColor", "SystemBaseMediumLowColor",
                "SystemAltHighColor", "SystemAltLowColor", "SystemAltMediumColor", "SystemAltMediumHighColor", "SystemAltMediumLowColor",
                "SystemChromeAltLowColor", "SystemChromeDisabledHighColor", "SystemChromeDisabledLowColor", "SystemListLowColor", "SystemListMediumColor",
                "SystemChromeHighColor", "SystemChromeLowColor", "SystemChromeMediumColor", "SystemChromeMediumLowColor"
            })
            {
                var b = Windows.UI.Xaml.Application.Current.Resources[r];
                Colors[r] = Color.FromHex(b.ToString());
            }

            // The root page of your application
            MainPage = new NavigationPage(new WebViewPage());
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
