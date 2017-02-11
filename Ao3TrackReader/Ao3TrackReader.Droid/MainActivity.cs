using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace Ao3TrackReader.Droid
{
	[Activity (Label = "Ao3TrackReader", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

            switch (Ao3TrackReader.App.Theme)
            {
                case "light":
                    SetTheme(Ao3TrackReader.Droid.Resource.Style.LightTheme);
                    break;

                case "dark":
                    SetTheme(Ao3TrackReader.Droid.Resource.Style.DarkTheme);
                    break;
            }
            

            global::Xamarin.Forms.Forms.Init (this, bundle);

			LoadApplication (new Ao3TrackReader.App ());
            

            var x = typeof(Xamarin.Forms.Themes.DarkThemeResources);
			x = typeof(Xamarin.Forms.Themes.LightThemeResources);
			x = typeof(Xamarin.Forms.Themes.Android.UnderlineEffect);
		}
	}
}

