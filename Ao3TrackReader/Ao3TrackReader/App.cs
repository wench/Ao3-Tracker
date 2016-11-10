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
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.AllowAutoRedirect = false;
            HttpClient = new HttpClient(httpClientHandler);
            Storage = new SyncedStorage();

            // The root page of your application
            MainPage = new NavigationPage(new WebViewPage());

            Data.Ao3SiteDataLookup.Lookup(new[] { @"http://archiveofourown.org/works/8398573", @"http://archiveofourown.org/works?utf8=%E2%9C%93&work_search%5Bsort_column%5D=revised_at&work_search%5Brating_ids%5D%5B%5D=11&work_search%5Bwarning_ids%5D%5B%5D=16&work_search%5Bwarning_ids%5D%5B%5D=14&work_search%5Bcategory_ids%5D%5B%5D=116&work_search%5Bfandom_ids%5D%5B%5D=1635478&work_search%5Bcharacter_ids%5D%5B%5D=3553370&work_search%5Brelationship_ids%5D%5B%5D=4001273&work_search%5Brelationship_ids%5D%5B%5D=2499660&work_search%5Bfreeform_ids%5D%5B%5D=18154&work_search%5Bother_tag_names%5D=Clarke+Griffin%2COctavia+Blake&work_search%5Bquery%5D=-omega&work_search%5Blanguage_id%5D=1&work_search%5Bcomplete%5D=0&commit=Sort+and+Filter&tag_id=Clarke+Griffin*s*Lexa" });
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
