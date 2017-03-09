/*
Copyright 2017 Alexis Ryan

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

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
    public enum InteractionMode
    {
        Unknown = -1,
        Phone = 0,
        Tablet = 1,
        PC = 2
    }

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

                case Device.iOS:
                    return name + dl;

                default:
                    throw new NotSupportedException("'" + Device.RuntimePlatform + "' is not a supported platform. Update Ao3TrackReader.App.PlatformIcon()");
            }
        }


        public static InteractionMode GetInteractionMode()
        {
#if WINDOWS_UWP
            if (Windows.Foundation.Metadata.ApiInformation.IsEventPresent("Windows.Phone.UI.Input.HardwareButtons", "BackPressed"))
            {
                if (typeof(Windows.Phone.UI.Input.HardwareButtons) == null)
                {
                    var eh = new EventHandler<Windows.Phone.UI.Input.BackPressedEventArgs>((sender, e) => { });
                    Windows.Phone.UI.Input.HardwareButtons.BackPressed += eh;
                    Windows.Phone.UI.Input.HardwareButtons.BackPressed -= eh;
                }
                return InteractionMode.Phone;
            }
            if (Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.UI.ViewManagement.UIViewSettings", "UserInteractionMode"))
            {
                var s = Windows.UI.ViewManagement.UIViewSettings.GetForCurrentView();

                if (s.UserInteractionMode == Windows.UI.ViewManagement.UserInteractionMode.Touch)
                    return InteractionMode.Tablet;
                else if (s.UserInteractionMode == Windows.UI.ViewManagement.UserInteractionMode.Mouse)
                    return InteractionMode.PC;
            }
#elif __ANDROID__
            // Good enough for android for now
            switch (Xamarin.Forms.Device.Idiom)
            {
                case TargetIdiom.Phone:
                    return InteractionMode.Phone;

                case TargetIdiom.Tablet:
                    return InteractionMode.Tablet;

                case TargetIdiom.Desktop:
                    return InteractionMode.PC;
            }
#endif

            return InteractionMode.Unknown;
        }

        public static bool HaveOSBackButton
        {
            get
            {
                var mode = GetInteractionMode();
                return mode == InteractionMode.Phone || mode == InteractionMode.Tablet;
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
