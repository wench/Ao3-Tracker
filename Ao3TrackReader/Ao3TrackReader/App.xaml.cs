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
using Ver = Ao3TrackReader.Version.Version;
using System.Threading.Tasks;

#if __WINDOWS__
using Windows.Storage;
#else
using System.IO;
#endif

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
        public static (int Major, int Minor, int Build) Version
        {
            get
            {
#if true
                return (Ver.Major, Ver.Minor, Ver.Build);
#else
#if __WINDOWS__
                var v = Windows.ApplicationModel.Package.Current.Id.Version;
                return (v.Major,v.Minor,v.Build);
#elif __ANDROID__
                //Android.Resource.Attribute.VersionCode
                var Context = Android.App.Application.Context.ApplicationContext;
                var pm = Context.PackageManager;
                var pi = pm.GetPackageInfo(Context.PackageName, 0);
                var v = pi.VersionName.ToString().Split('.');
                return (int.Parse(v[0]), int.Parse(v[1]), int.Parse(v[2]));
#elif __IOS__
                var bundle = Foundation.NSBundle.MainBundle;
                var vi = bundle.ObjectForInfoDictionary("CFBundleVersion");
                var v = vi.ToString().Split('.');
                return (int.Parse(v[0]), int.Parse(v[1]), int.Parse(v[2]));
#else
                return (0,0,0);
#endif
#endif
            }
        }

        public static string Copyright
        {
            get
            {
                return Ver.Copyright;
            }
        }

        public static (string Name, Uri uri) License
        {
            get
            {
                return ("Apache License, Version 2.0", new Uri("https://www.apache.org/licenses/LICENSE-2.0"));
            }
        }

        public static Uri Source
        {
            get
            {
#if DEBUG
                if (!string.IsNullOrWhiteSpace(Ver.GitRevision))
                    return new Uri("https://github.com/wench/Ao3-Tracker/tree/" + Ver.GitRevision);
                else
                    return new Uri("https://github.com/wench/Ao3-Tracker");
#else
                return new Uri("https://github.com/wench/Ao3-Tracker/releases/v" + Ver.Major + "." + Ver.Minor + "." + Ver.Build);
#endif
            }
        }


        public static Ao3TrackDatabase Database
        {
            get; private set;
        }

        public static SyncedStorage Storage
        {
            get; private set;
        }

        public new NavigationPage MainPage
        {
            get { return (NavigationPage)base.MainPage; }
            set { base.MainPage = value; }
        }

        public WebViewPage WebViewPage
        {
            get { return wvp;}
        }

        public new static App Current
        {
            get { return (App)Xamarin.Forms.Application.Current; }
        }

        static bool _LoggedError = false;
        public static void Log(Exception e)
        {
            // Anything we do here must be wrapped, cause the app might be in an impossible state
            try
            {
                if (_LogErrors && !_LoggedError)
                {
                    _LoggedError = true;

                    var settings = new Newtonsoft.Json.JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                        ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Auto,
                        TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects,
                        Formatting = Newtonsoft.Json.Formatting.Indented
                    };
                    string report = Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        Platform = Xamarin.Forms.Device.RuntimePlatform,
                        Mode = GetInteractionMode().ToString(),
                        Version = Ao3TrackReader.Version.Version.LongString,
                        Date = DateTime.UtcNow,
                        Exception = e
                    }, settings);

                    TextFileSave("ErrorReport.json", report);
                }
            }
            catch
            {
            }
        }

        static bool _LogErrors;
        public static bool LogErrors
        {
            get { return _LogErrors; }
            set { Database.SaveVariable("LogErrors", _LogErrors = value); }
        }

        static App()
        {
            Database = new Ao3TrackDatabase();
            Database.TryGetVariable("LogErrors", bool.TryParse, out _LogErrors, true);
            Storage = new SyncedStorage();

            Task.Run(async () =>
            {
                var report = await TextFileLoadAsync("ErrorReport.json");
                TextFileDelete("ErrorReport.json");

                if (_LogErrors)
                {
                    if (!string.IsNullOrWhiteSpace(report))
                    {
                        await Storage.SubmitErrorReport(report);
                    }
                }
            });

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

                case "UWP":
                case "Windows":
                case "WinRT":
                case "WinPhone":
                    return "Assets/" + name + dl;

                case Device.iOS:
                    return name + dl;

                default:
                    throw new NotSupportedException("'" + Device.RuntimePlatform + "' is not a supported platform. Update Ao3TrackReader.App.PlatformIcon()");
            }
        }

#if WINDOWS_UWP
static bool PhoneHasBackButton()
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                if (typeof(Windows.Phone.UI.Input.HardwareButtons) == null)
                {
                    var eh = new EventHandler<Windows.Phone.UI.Input.BackPressedEventArgs>((sender, e) => { });
                    Windows.Phone.UI.Input.HardwareButtons.BackPressed += eh;
                    Windows.Phone.UI.Input.HardwareButtons.BackPressed -= eh;
                }
                return true;
            }
            return false;
        }
#endif

        public static InteractionMode GetInteractionMode()
        {
#if WINDOWS_UWP
    var s = Windows.UI.ViewManagement.UIViewSettings.GetForCurrentView();
            if (s.UserInteractionMode == Windows.UI.ViewManagement.UserInteractionMode.Mouse)
                return InteractionMode.PC;

            try
            { 
                if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile" && PhoneHasBackButton())
                    return InteractionMode.Phone;
            }
            catch {

            }

            if (s.UserInteractionMode == Windows.UI.ViewManagement.UserInteractionMode.Touch)
                return InteractionMode.Tablet;
#elif WINDOWS_APP
                return InteractionMode.PC;
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

        public static T RunSynchronously<T>(System.Threading.Tasks.Task<T> task)
        {
            task.Wait();
            return task.Result;
        }

        public static void RunSynchronously(System.Threading.Tasks.Task task)
        {
            task.Wait();
        }

#if __WINDOWS__
        public static T RunSynchronously<T>(Windows.Foundation.IAsyncOperation<T> iasync)
        {
            var task = iasync.AsTask();
            task.Wait();
            return task.Result;
        }

        public static void RunSynchronously(Windows.Foundation.IAsyncAction iasync)
        {
            var task = iasync.AsTask();
            task.Wait();
        }

        public static void TextFileSave(string filename, string text)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var sampleFile = RunSynchronously(localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting));
            RunSynchronously(FileIO.WriteTextAsync(sampleFile, text));
        }
        public static async Task<string> TextFileLoadAsync(string filename)
        {
            var storageFolder = ApplicationData.Current.LocalFolder;
            var sampleFile = (await storageFolder.TryGetItemAsync(filename)) as StorageFile;
            if (sampleFile == null) return null;
            return await Windows.Storage.FileIO.ReadTextAsync(sampleFile);
        }
        public static void TextFileDelete(string filename)
        {
            var storageFolder = ApplicationData.Current.LocalFolder;
            var sampleFile = RunSynchronously(storageFolder.TryGetItemAsync(filename)) as StorageFile;
            if (sampleFile != null) RunSynchronously(sampleFile.DeleteAsync());
        }
#else
        public static void TextFileSave(string filename, string text)
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var filePath = Path.Combine(documentsPath, filename);
            System.IO.File.WriteAllText(filePath, text);
        }
        public static Task<string> TextFileLoadAsync(string filename)
        {
            return Task.Run(()=> { 
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                var filePath = Path.Combine(documentsPath, filename);
                return System.IO.File.ReadAllText(filePath);
            });
        }
        public static void TextFileDelete(string filename)
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var filePath = Path.Combine(documentsPath, filename);
            System.IO.File.Delete(filePath);
        }
#endif

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
