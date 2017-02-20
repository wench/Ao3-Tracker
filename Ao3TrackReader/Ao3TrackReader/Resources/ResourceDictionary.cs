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
#if !WINDOWS_UWP
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
                Add(cat.Name + "Color", color);
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

                    Add(kp.Key + prop.Name + "Color", color);
#if WINDOWS_UWP
                    md[kp.Key + prop.Name] = new Windows.UI.Xaml.Media.SolidColorBrush(color.ToWindows());
#endif
                }

            }

            foreach (var prop in typeof(Icons).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                var icon = (string)prop.GetValue(null);
                Add(prop.Name + "Icon", icon);
#if WINDOWS_UWP
                md[prop.Name + "Icon"] = icon;
#endif
            }


        }
    }
}
