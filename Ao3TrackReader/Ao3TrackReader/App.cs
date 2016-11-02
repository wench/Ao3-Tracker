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
        static Ao3TrackDatabase database;

        public static Ao3TrackDatabase Database
        {
            get
            {
                database = database ?? new Ao3TrackDatabase();
                return database;
            }
        }

        static SyncedStorage storage;
        public static SyncedStorage Storage
        {
            get
            {
                storage = storage ?? new SyncedStorage();
                return storage;
            }
        }

        static readonly HttpClient httpClient;
        public static HttpClient HttpClient
        {
            get
            {
                return httpClient;
            }
        }



        static App()
        {
            httpClient = new HttpClient();
        }

        public App()
        {
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
