using Ao3TrackReader.Data;
using Ao3TrackReader.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;

using Xamarin.Forms;
using Button = Ao3TrackReader.Controls.Button;

namespace Ao3TrackReader
{
    public partial class App : Application
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
            Database = new Ao3TrackDatabase();
            Storage = new SyncedStorage();

            Theme = Ao3TrackReader.App.Database.GetVariable("Theme");
            if (string.IsNullOrWhiteSpace(Theme)) Theme = "light";
        }

        public static string Theme { get; private set; }
        public static string PlatformIcon(string name)
        {
            string dl = "_" + Theme;
            return Device.OnPlatform(name + dl, name + dl, "Assets/" + name + dl);
        }

        WebViewPage wvp;

        public App()
        {
            InitializeComponent();

            // The root page of your application
            MainPage = new NavigationPage(wvp = new WebViewPage());
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            wvp.OnSleep();
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
            wvp.OnResume();
        }
    }
}
