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
using System.Linq;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Ao3TrackReader.Controls;
using Xamarin.Forms.Platform.Android;

namespace Ao3TrackReader.Droid
{
    [Activity(Label = "Ao3TrackReader", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            switch (Ao3TrackReader.App.Theme)
            {
                case "light":
                    SetTheme(Ao3TrackReader.Droid.Resource.Style.LightTheme);
                    break;

                case "dark":
                    SetTheme(Ao3TrackReader.Droid.Resource.Style.DarkTheme);
                    break;
            }
            base.OnCreate(bundle);
            global::Xamarin.Forms.Forms.Init(this, bundle);

            var app = new Ao3TrackReader.App();
            LoadApplication(app);
            app.MainPage.SizeChanged += MainPage_SizeChanged;

            var x = typeof(Xamarin.Forms.Themes.DarkThemeResources);
            x = typeof(Xamarin.Forms.Themes.LightThemeResources);
            x = typeof(Xamarin.Forms.Themes.Android.UnderlineEffect);
        }

        private void MainPage_SizeChanged(object sender, EventArgs e)
        {
            InvalidateOptionsMenu();
        }

        private void UpdateColorTint(Android.Views.IMenuItem item, Ao3TrackReader.Controls.ToolbarItem toolbaritem)
        {
            Xamarin.Forms.Color? color = null;

            if (item.IsEnabled == false)
            {
                color = Ao3TrackReader.Resources.Colors.Base.MediumLow;
            }
            else if (toolbaritem != null && toolbaritem.Foreground != Xamarin.Forms.Color.Default)
            {
                color = toolbaritem.Foreground;
            }

            if (color != null)
            {
                if (item.Icon != null) item.Icon.SetColorFilter(new Android.Graphics.Color(
                        (byte)(color.Value.R * 255),
                        (byte)(color.Value.G * 255),
                        (byte)(color.Value.B * 255)
                    ), Android.Graphics.PorterDuff.Mode.SrcIn);
                var tt = new Models.TextNode { Text = item.TitleFormatted.ToString() };
                var spannable = tt.ConvertToSpannable(new Models.StateNode { Foreground = color });
                item.SetTitle(spannable);
            }
            else
            {
                if (item.Icon != null) item.Icon.ClearColorFilter();
                item.SetTitle(item.TitleFormatted.ToString());
            }

        }

        private void Toolbaritem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Foreground")
            {
                var toolbaritem = sender as Ao3TrackReader.Controls.ToolbarItem;
                UpdateColorTint(toolbaritem.MenuItem, toolbaritem);
            }
        }

        class ClickListener : Java.Lang.Object, IMenuItemOnMenuItemClickListener
        {
            private Xamarin.Forms.IMenuItemController xitem;

            public ClickListener(Xamarin.Forms.IMenuItemController menuitem)
            {
                xitem = menuitem;
            }

            public bool OnMenuItemClick(IMenuItem item)
            {
                xitem.Activate();
                return true;
            }
        }


        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            var source = App.Current.MainPage.CurrentPage.ToolbarItems;

            for (var i = 0; i < menu.Size(); i++)
            {
                var item = menu.GetItem(i);
                if ((item.Order & (int)MenuCategory.System) != (int)MenuCategory.System && item.Icon is Android.Graphics.Drawables.BitmapDrawable)
                {
                    if (source.Where((t) => t.Text == item.TitleFormatted.ToString()).FirstOrDefault() is Ao3TrackReader.Controls.ToolbarItem toolbaritem)
                    {
                        toolbaritem.PropertyChanged += Toolbaritem_PropertyChanged;
                        toolbaritem.MenuItem = null;
                    }
                }
            }

            bool res = base.OnPrepareOptionsMenu(menu);

            int remaining = (int)App.Current.MainPage.Width;
            remaining -= 50;    // App icon
            remaining -= 50;    // Overflow button
            int count = 0;
            ISubMenu submenu = null;

            if (Android.OS.Build.VERSION.SdkInt < BuildVersionCodes.Kitkat && remaining < 250)
            {
                submenu = menu.AddSubMenu("More");
                submenu.SetIcon(Android.Resource.Drawable.IcMenuMore);
                submenu.Item.SetShowAsAction(ShowAsAction.Always);
            }

            for (var i = 0; i < menu.Size(); i++)
            {
                var item = menu.GetItem(i);
                if ((item.Order & 0xFFFF0000) == 0 && !item.HasSubMenu)
                {
                    var toolbaritem = source.Where((t) => t.Text == item.TitleFormatted.ToString()).FirstOrDefault() as Ao3TrackReader.Controls.ToolbarItem;
                    if (toolbaritem != null)
                    {
                        toolbaritem.PropertyChanged += Toolbaritem_PropertyChanged;
                        toolbaritem.MenuItem = item;

                        if (item.Icon == null && toolbaritem.Icon != null)
                        {
                            item.SetIcon(new Android.Graphics.Drawables.BitmapDrawable(Resources, Resources.GetBitmap(toolbaritem.Icon)));
                        }
                    }
                    
                    UpdateColorTint(item, toolbaritem);

                    if (item.Icon != null)
                    {
                        int iconsize = 50;
                        if (remaining >= iconsize)
                        {
                            count++;
                            remaining -= iconsize;
                            item.SetShowAsAction(ShowAsAction.Always);
                        }
                        else if (submenu != null && toolbaritem != null)
                        {
                            var newitem = submenu.Add(item.GroupId, item.ItemId, item.Order, item.TitleFormatted);
                            newitem.SetIcon(item.Icon);
                            newitem.SetEnabled(item.IsEnabled);
                            newitem.SetOnMenuItemClickListener(new ClickListener(toolbaritem));
                            toolbaritem.MenuItem = newitem;
                            item.SetVisible(false);
                        }
                        else
                        {
                            item.SetShowAsAction(ShowAsAction.IfRoom);
                            remaining = 0;
                        }
                    }
                }
            }

            return res;
        }
    }
}

