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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xamarin.Forms;

namespace Ao3TrackReader.Resources
{
    public class ResourceDictionary : Xamarin.Forms.ResourceDictionary
    {
        public ResourceDictionary()
        {
            App.Database.GetVariableEvents("LogFontSizeUI").Updated += ResourceDictionary_Updated;
            App.Database.TryGetVariable("LogFontSizeUI", int.TryParse, out int LogFontSizeUI, 0);
            UpdateFontsize(LogFontSizeUI);

#if !__WINDOWS__
            // Andriod and iOS uses Xamarin Forms theme           
            switch (App.Theme)  
            {
                default:
                case "light":
                    MergedWith = typeof(Xamarin.Forms.Themes.LightThemeResources);
                    break;

                case "dark":
                    MergedWith = typeof(Xamarin.Forms.Themes.DarkThemeResources);
                    break;
            }
#else
            var thm = App.Theme.Substring(0, 1).ToUpper() + App.Theme.Substring(1);
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
                    if (prop.GetValue(set) is BaseColorSet subset) sets.Add(cat.Name + prop.Name, subset);
                }

                var color = (Color)set;
                Add(cat.Name + "Color", color);
#if __WINDOWS__
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

                    Add(kp.Key + prop.Name + "Color", color);
#if __WINDOWS__
                    md[kp.Key + prop.Name + "Color"] = new Windows.UI.Xaml.Media.SolidColorBrush(color.ToWindows());
#endif
                }

            }

            foreach (var prop in typeof(Icons).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                var icon = (string)prop.GetValue(null);
                Add(prop.Name + "Icon", icon);
#if __WINDOWS__
                md[prop.Name + "Icon"] = icon;
#endif
            }


        }

        private void ResourceDictionary_Updated(object sender, Ao3TrackDatabase.VariableUpdatedEventArgs e)
        {
            if (!int.TryParse(e.NewValue, out int LogFontSizeUI)) LogFontSizeUI = 0;
            UpdateFontsize(LogFontSizeUI);
        }

        public void UpdateFontsize(int LogFontSizeUI)
        {
            this["HugeFontSize"] = 24.0 * Math.Pow(1.05, LogFontSizeUI);
            this["LargeFontSize"] = 22.0 * Math.Pow(1.05, LogFontSizeUI);
            this["MediumFontSize"] = 18.0 * Math.Pow(1.05, LogFontSizeUI);
            this["MediumSmallFontSize"] = 16.0 * Math.Pow(1.05, LogFontSizeUI);
            this["SmallFontSize"] = 14.0 * Math.Pow(1.05, LogFontSizeUI);
            this["MicroFontSize"] = 11.0 * Math.Pow(1.05, LogFontSizeUI);
            this["TinyFontSize"] = 10.0 * Math.Pow(1.05, LogFontSizeUI);

            for (int i = 1; i <= 100; i++)
            {
                double size = i * Math.Pow(1.05, LogFontSizeUI);
                this["Size_" + i] = size;
                this["Size_" + i + "_Min"] = Math.Max(size, i);
            }
        }
    }
}
