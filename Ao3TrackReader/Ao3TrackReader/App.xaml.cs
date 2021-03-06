﻿/*
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
using System.Threading.Tasks;
using Ver = Ao3TrackReader.Version.Version;

[assembly:Xamarin.Forms.Xaml.XamlCompilation(Xamarin.Forms.Xaml.XamlCompilationOptions.Compile)]

namespace Ao3TrackReader
{
    public enum InteractionMode
    {
        Unknown = -1,
        Phone = 0,
        Tablet = 1,
        PC = 2
    }

    public enum Architechture
    {
        Any = 0,
        x86 = 1,
        x64 = 2,
        ARM = 3,
        ARM64 = 4
    }

    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;

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

        static bool _LoggedError = false;

        public static void Log(object e)
        {
            // Anything we do here must be wrapped, cause the app might be in an impossible state
            if (System.Diagnostics.Debugger.IsAttached)
                {
                System.Diagnostics.Debug.Write(e);
                System.Diagnostics.Debugger.Break();
            }
            try
            {
                if (_LogErrors)
                {
                    var settings = new Newtonsoft.Json.JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                        ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Auto,
                        TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All,
                        Formatting = Newtonsoft.Json.Formatting.Indented,   
                        MetadataPropertyHandling = Newtonsoft.Json.MetadataPropertyHandling.ReadAhead,
                        ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver() { IgnoreSerializableAttribute = true, IgnoreSerializableInterface = true }
                    };
                    var toSerialize = new
                    {
                        Platform = Device.RuntimePlatform,
                        Mode = InteractionMode.ToString(),
                        Version = Ver.String,
                        GitRev = Ver.GitRevision,
                        GetTag = Ver.GitTag,
                        Arch = BuildArchitechture.ToString(),
                        OSName = OSName,
                        OSVersion = OSVersion,
                        OSArch = OSArchitechture,
                        HWType = HardwareType,
                        HWName = HardwareName,
                        Date = DateTime.UtcNow,
                        Exception = e
                    };

                    string report = Newtonsoft.Json.JsonConvert.SerializeObject(toSerialize, settings);

                    if (_LoggedError) report = ",\n" + report;

                    TextFileSave("ErrorReport.json", report, _LoggedError);
                    _LoggedError = true;
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
                try
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
                }
                catch (Exception)
                {
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

        public static Architechture BuildArchitechture
        {
            get
            {
#if __X86__
                return Architechture.x86;
#elif __X64__
                return Architechture.x64;
#elif __ARM__
                return Architechture.ARM;
#elif __ARM64__
                return Architechture.ARM64;
#else
                return Architechture.Any;
#endif
            }
        }

        public static bool HaveOSBackButton
        {
            get
            {
                var mode = App.InteractionMode;
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

        public App(Uri startPage, bool networkstate)
        {
            HaveNetwork = networkstate;
            InitializeComponent();

            // The root page of your application
            MainPage = new NavigationPage();
            MainPage.PushAsync(new WebViewPage(startPage), false);
            
            if (HaveNetwork)
            {
                CheckVersion(this, new EventArgs<bool>(true));
            }
            else
            {
                HaveNetworkChanged += CheckVersion;
            }
        }

        private async void CheckVersion(object sender, EventArgs<bool> e)
        {
            if (e.Value)
            {
                HaveNetworkChanged -= CheckVersion;

                try
                {
                    var sversions = await Storage.FetchAsync("version.json");

                    if (string.IsNullOrEmpty(sversions))
                        return;

                    var versions = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, SortedDictionary<double, int[]>>>(sversions);

                    if (versions.TryGetValue(Device.RuntimePlatform,out var pversions))
                    {
                        var latest = pversions.Where((kp) => kp.Key <= OSBuild).Last().Value;
                        if (Version.Version.Integer < Version.Version.AsInteger(latest[0], latest[1], latest[2], latest[3]))
                        {
                            WebViewPage.Current.ShowError("An update to Archive Track Reader available. Check in the Store to download.");
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            WebViewPage.Current.OnSleep();
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
            WebViewPage.Current.OnResume();
        }

        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.IsNullOrEmpty(propertyName) || propertyName == "HaveNetwork")
                OnHaveNetworkChanged();                
        }

        public static readonly Xamarin.Forms.BindableProperty HaveNetworkProperty =
          Xamarin.Forms.BindableProperty.Create("HaveNetwork", typeof(bool), typeof(App), defaultValue: false);

        public bool HaveNetwork
        {
            get { return (bool)GetValue(HaveNetworkProperty); }
            internal set { SetValue(HaveNetworkProperty, value); }
        }

        public event EventHandler<EventArgs<bool>> HaveNetworkChanged;     

        void OnHaveNetworkChanged()
        {
            bool have = HaveNetwork;
            if (have) Storage.DoSyncAsync();
            HaveNetworkChanged?.Invoke(this, new EventArgs<bool>(have));
        }
    }
}
