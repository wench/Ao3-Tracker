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

        public new NavigationPage MainPage {
            get { return (NavigationPage) base.MainPage; }
            set { base.MainPage = value; }
        }

        public new static App Current
        {
            get { return (App) Xamarin.Forms.Application.Current; }
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
            switch (Device.RuntimePlatform)
            {
                case Device.Android:
                    return name + dl;

                case Device.Windows:
                case Device.WinPhone:
                case "UWP":
                    return "Assets/" + name + dl;

                default:
                    throw new NotSupportedException("'" + Device.RuntimePlatform + "' is not a supported platform. Update Ao3TrackReader.App.PlatformIcon()");
            }
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
