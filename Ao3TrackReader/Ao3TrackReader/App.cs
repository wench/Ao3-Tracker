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
            Resources = new ResourceDictionary();
#if !WINDOWS_UWP
            // Andriod and iOS uses Xamarin Forms theme           
            switch (Theme)
            {
                default:
                case "light":
                    Resources.MergedWith = typeof(Xamarin.Forms.Themes.LightThemeResources);
                    break;

                case "dark":
                    Resources.MergedWith = typeof(Xamarin.Forms.Themes.DarkThemeResources);
                    break;
            }

            
#endif
            var sets = new Dictionary<string, BaseColorSet>();
            foreach (var cat in typeof(Resources.Colors).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                ColorSet set = (ColorSet)cat.GetValue(null);
                sets.Add(cat.Name, set);

                foreach (var prop in set.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var subset = prop.GetValue(set) as BaseColorSet;
                    if (subset != null) sets.Add(cat.Name + prop.Name, subset);
                }

                Resources.Add(cat.Name + "Color", (Color)set);
            }

            foreach (var kp in sets)
            {
                foreach (var prop in kp.Value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                {
                    Resources.Add(kp.Key+prop.Name + "Color", prop.GetValue(kp.Value));
                }

            }

            foreach (var prop in typeof(Resources.Icons).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                var icon = (string)prop.GetValue(null);
                Resources.Add(prop.Name+ "Icon", icon);
            }


            Resources.Add("PaneImageButton", new Style(typeof(Button))
            {
                Setters = {
                    new Setter{ Property = Button.BorderWidthProperty, Value = 2.0 },
                    new Setter{ Property = Button.WidthRequestProperty, Value = 40.0 },
                    new Setter{ Property = Button.HeightRequestProperty, Value = 40.0 },
                    new Setter{ Property = Button.ImageWidthProperty, Value = 20.0 },
                    new Setter{ Property = Button.ImageHeightProperty, Value = 20.0 },
                }
            });

            // The root page of your application
            MainPage = new WVPNavigationPage(wvp = new WebViewPage()); 
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
