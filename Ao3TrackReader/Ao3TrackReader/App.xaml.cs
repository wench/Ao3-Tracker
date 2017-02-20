﻿using Ao3TrackReader.Data;
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
#else
            if (!Windows.UI.Xaml.Application.Current.Resources.ThemeDictionaries.TryGetValue("Default", out var omd))
            {
                Windows.UI.Xaml.Application.Current.Resources.ThemeDictionaries.Add("Default", omd = new Windows.UI.Xaml.ResourceDictionary());
            }
            var md = (Windows.UI.Xaml.ResourceDictionary)omd;
            md["SystemAccentColor"] = Colors.Highlight.Solid.High.ToWindows();
#endif

            var sets = new Dictionary<string, BaseColorSet>();
            foreach (var cat in typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                ColorSet set = (ColorSet)cat.GetValue(null);
                sets.Add(cat.Name, set);

                foreach (var prop in set.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var subset = prop.GetValue(set) as BaseColorSet;
                    if (subset != null) sets.Add(cat.Name + prop.Name, subset);
                }

                var color = (Color)set;
                Resources.Add(cat.Name + "Color", color);
#if WINDOWS_UWP
                md[cat.Name + "Color"] = new Windows.UI.Xaml.Media.SolidColorBrush(color.ToWindows());
#endif
            }

            foreach (var kp in sets)
            {
                foreach (var prop in kp.Value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                {
                    object o = prop.GetValue(kp.Value);
                    Color color;
                    if (o.GetType() == typeof(ColorSet))
                    {
                        color = (Color)(ColorSet)o;
                    }
                    else if (o.GetType() == typeof(Color))
                    {
                        color = (Color)o;
                    }
                    else
                    {
                        continue;
                    }

                    Resources.Add(kp.Key + prop.Name + "Color", color);
#if WINDOWS_UWP
                    md[kp.Key + prop.Name] = new Windows.UI.Xaml.Media.SolidColorBrush(color.ToWindows());
#endif
                }

            }

            foreach (var prop in typeof(Icons).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                var icon = (string)prop.GetValue(null);
                Resources.Add(prop.Name + "Icon", icon);
#if WINDOWS_UWP
                md[prop.Name + "Icon"] = icon;
#endif
            }

            Resources.Add(new Style(typeof(Button))
            {
                Setters = {
                    new Setter{ Property = Button.BackgroundColorProperty, Value = Color.Transparent },
                    new Setter{ Property = Button.TextColorProperty, Value = Colors.Base.High },
                    new Setter{ Property = Button.BorderWidthProperty, Value = 2.0 },
                    new Setter{ Property = Button.WidthRequestProperty, Value = 40.0 },
                    new Setter{ Property = Button.HeightRequestProperty, Value = 40.0 },
                    new Setter{ Property = Button.ImageWidthProperty, Value = 20.0 },
                    new Setter{ Property = Button.ImageHeightProperty, Value = 20.0 },
                }
            });

            Resources.Add(new Style(typeof(Label))
            {
                Setters = {
                    new Setter{Property = Label.TextColorProperty, Value = Colors.Base.Medium }
                }
            });
            Resources.Add(new Style(typeof(Controls.TextView))
            {
                Setters = {
                    new Setter{Property = Controls.TextView.TextColorProperty, Value = Colors.Base.Medium }
                }
            });


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