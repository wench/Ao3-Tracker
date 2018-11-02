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
using Java.Lang;
using Android.Util;
using Android.Net;
using Android.Content;

namespace Ao3TrackReader.Droid
{
    [Activity(Label = "Archive Track Reader", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, HardwareAccelerated = false)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity, Java.Lang.Thread.IUncaughtExceptionHandler
    {
        public class NetworkReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    var cm = (ConnectivityManager)context.GetSystemService(ConnectivityService);
                    NetworkInfo netInfo = cm.ActiveNetworkInfo;
                    App.Current.HaveNetwork = netInfo?.IsConnectedOrConnecting == true;
                });
            }
        }

        NetworkReceiver receiver;

        public static MainActivity Instance { get; private set; } = null;

        protected override void OnCreate(Bundle bundle)
        {
            //System.Diagnostics.Debugger.Break();
            switch (Ao3TrackReader.App.Theme)
            {
                case "light":
                    SetTheme(Ao3TrackReader.Droid.Resource.Style.LightTheme);
                    break;

                case "dark":
                    SetTheme(Ao3TrackReader.Droid.Resource.Style.DarkTheme);
                    break;
            }
            Java.Lang.Thread.DefaultUncaughtExceptionHandler = this;
            base.OnCreate(bundle);
            global::Xamarin.Forms.Forms.Init(this, bundle);
            Instance = this;

            var cm = (ConnectivityManager) GetSystemService(ConnectivityService);
            var netInfo = cm.ActiveNetworkInfo;

            var app = new App(null, netInfo?.IsConnectedOrConnecting == true);

            IntentFilter filter = new IntentFilter(ConnectivityManager.ConnectivityAction);
            receiver = new NetworkReceiver();
            RegisterReceiver(receiver, filter);

            LoadApplication(app);
            app.MainPage.SizeChanged += MainPage_SizeChanged;

            var x = typeof(Xamarin.Forms.Themes.DarkThemeResources);
            x = typeof(Xamarin.Forms.Themes.LightThemeResources);
            x = typeof(Xamarin.Forms.Themes.Android.UnderlineEffect);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (receiver != null) UnregisterReceiver(receiver);
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
                var tt = new Text.String { Text = item.TitleFormatted.ToString() };
                var renderer = Xamarin.Forms.Platform.Android.Platform.GetRenderer(App.Current.MainPage);                
                var spannable = tt.ConvertToSpannable(new Text.StateNode { Foreground = color }, renderer.View.Resources.DisplayMetrics);
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

            var submenu = menu.AddSubMenu("More");
            submenu.SetIcon(Ao3TrackReader.App.Theme=="dark"?Resource.Drawable.more_dark:Resource.Drawable.more_light);
            submenu.Item.SetShowAsAction(ShowAsAction.Always);
            bool hasprimary = false;
            bool hassecondary = false;

            for (var i = 0; i < menu.Size(); i++)
            {
                var item = menu.GetItem(i);
                if ((item.Order & 0xFFFF0000) == 0 && !item.HasSubMenu)
                {
                    bool isprimary = item.Icon != null;
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

                    if (isprimary && remaining >= 50)
                    {
                        remaining -= 50;
                        item.SetShowAsAction(ShowAsAction.Always);
                    }
                    else if (submenu != null && toolbaritem != null)
                    {
                        var newitem = submenu.Add(item.GroupId, item.ItemId, item.Order + (isprimary?0:1024), item.TitleFormatted);
                        if (isprimary) hasprimary = true;
                        else hassecondary = true;
                        newitem.SetIcon(item.Icon);
                        newitem.SetEnabled(item.IsEnabled);
                        newitem.SetOnMenuItemClickListener(new ClickListener(toolbaritem));
                        toolbaritem.MenuItem = newitem;
                        item.SetVisible(false);                        
                    }
                    else
                    {
                        item.SetShowAsAction(ShowAsAction.Never);
                        remaining = 0;
                    }
                }
            }
            if (hasprimary && hassecondary)
            {
                submenu.Add(Menu.None, Menu.None, 1023, "\x23AF\x23AF\x23AF\x23AF").SetEnabled(false);
            }

            return res;
        }

        void Thread.IUncaughtExceptionHandler.UncaughtException(Thread t, Throwable e)
        {
            App.Log(e);
        }
    }
}

