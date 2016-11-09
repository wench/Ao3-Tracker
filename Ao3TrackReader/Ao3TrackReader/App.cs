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

        public static HttpClient HttpClient
        {
            get; private set;
        }



        static App()
        {
        }

        public App()
        {
            Database = new Ao3TrackDatabase();
            HttpClient = new HttpClient();
            Storage = new SyncedStorage();

            // The root page of your application
            MainPage = new NavigationPage(new WebViewPage());

            //Data.Ao3SiteDataLookup.Lookup(new[] { @"http://archiveofourown.org/works/8398573" });
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
