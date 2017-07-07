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
            App.Database.TryGetVariable("LogFontSizeUI", int.TryParse, out int LogFontSizeUI);
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
            foreach (var cat in typeof(Colors).GetRuntimeProperties())
            {
                ColorSet set = cat.GetValue(null) as ColorSet;
                if (set == null) continue;
                sets.Add(cat.Name, set);

                foreach (var prop in set.GetType().GetRuntimeProperties())
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
                foreach (var prop in kp.Value.GetType().GetRuntimeProperties())
                {
                    object o = prop.GetValue(kp.Value);
                    if (o is null) continue;
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

            foreach (var prop in typeof(Icons).GetRuntimeProperties())
            {
                var icon = prop.GetValue(null) as string;
                if (icon is null) continue;
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
            double multiplier = Math.Pow(1.05, LogFontSizeUI);
            this["HugeFontSize"] = 24.0 * multiplier;
            this["LargeFontSize"] = 22.0 * multiplier;
            this["MediumFontSize"] = 18.0 * multiplier;
            this["MediumSmallFontSize"] = 16.0 * multiplier;
            this["SmallFontSize"] = 14.0 * multiplier;
            this["MicroFontSize"] = 11.0 * multiplier;
            this["TinyFontSize"] = 10.0 * multiplier;

            for (int i = 1; i <= 100; i++)
            {
                double size = i * multiplier;
                this["Size_" + i] = size;
                this["Size_" + i + "_Min"] = Math.Max(size, i);
            }
        }
    }
}
