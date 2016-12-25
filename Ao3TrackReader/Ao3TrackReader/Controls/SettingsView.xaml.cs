using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Ao3TrackReader.Resources;

namespace Ao3TrackReader.Controls
{
    public partial class SettingsView : PaneView
    {
        bool isCreateUser;
        WebViewPage wpv;
        Dictionary<string, string> themes;

        public SettingsView (WebViewPage wpv)
        {
            this.wpv = wpv;
            InitializeComponent();

            BackgroundColor = Colors.Alt.Trans.High;
            isCreateUser = false;

            themes = new Dictionary<string, string> {
                { "light", "Light" },
                { "dark", "Dark" }
            };
            themeList.ItemsSource = themes.Values;
        }

        protected override void OnIsOnScreenChanging(bool newValue)
        {
            if (newValue == true)
            {
                UpdateSyncForm();
                SelectCurrentTheme();
                httpsSwitch.IsToggled = Data.Ao3SiteDataLookup.UseHttps;
            }
        }

        public void OnClose(object sender, EventArgs e)
        {
            IsOnScreen = false;
        }

        public void OnHttpsSwitch(object sender, EventArgs e)
        {
            Data.Ao3SiteDataLookup.UseHttps = httpsSwitch.IsToggled;
        }

        public void OnSyncLogin(object sender, EventArgs e)
        {
            isCreateUser = false;
            UpdateSyncForm();
        }

        public void OnSyncCreate(object sender, EventArgs e)
        {
            isCreateUser = true;
            UpdateSyncForm();
        }

        public void OnSyncSubmit(object sender, EventArgs e)
        {
            syncSubmitButton.IsEnabled = false;
            syncIndicator.IsRunning = true;
            syncIndicator.IsVisible = true;

            Task.Run(async () =>
            {
                Dictionary<string, string> errors = null;
                if (isCreateUser)
                {
                }
                else
                {
                    errors = await App.Storage.Login(username.Text, password.Text);
                }
                wpv.DoOnMainThread(() => 
                { 
                    if (errors != null && errors.Count > 0)
                    {
                    }
                    else
                    {
                        UpdateSyncForm();
                        wpv.ReadingList.SyncToServerAsync();
                    }
                    syncSubmitButton.IsEnabled = true;
                    syncIndicator.IsRunning = false;
                    syncIndicator.IsVisible = false;
                });
            });

        }

        public void OnSyncLogout(object sender, EventArgs e)
        {
        }

        void SelectCurrentTheme()
        {
            string theme = Ao3TrackReader.App.Database.GetVariable("Theme");
            if (string.IsNullOrWhiteSpace(theme)) theme = "light";
            string name;
            if (!themes.TryGetValue(theme, out name))
            {
                var pair = themes.First();
                theme = pair.Key;
                name = pair.Value;
            }
            App.Database.SaveVariable("Theme",theme);
            themeList.SelectedItem = name;
        }

        void OnThemeSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
            {
                SelectCurrentTheme();
                return;
            }
            string name = (string)e.SelectedItem;
            string theme = themes.Where((pair) => pair.Value == name).Select((pair) => pair.Key).FirstOrDefault();
            if (theme != null)
            {
                App.Database.SaveVariable("Theme", theme);
            }
        }

        private void UpdateSyncForm()
        {
            string name = App.Storage.Username;
            if (!string.IsNullOrWhiteSpace(name))
            {
                syncLoggedIn.IsVisible = true;
                syncForm.IsVisible = false;
                var text = new FormattedString();
                text.Spans.Add(new Span { Text = "Logged in as: " });
                text.Spans.Add(new Span { Text = name, ForegroundColor = Colors.Highlight.Low });
                syncLoggedInLabel.FormattedText = text;
            }
            else
            {
                syncLoggedIn.IsVisible = false;
                syncForm.IsVisible = true;
                if (isCreateUser)
                {
                    syncLoginButton.BorderColor = Colors.Base.Trans.Low;
                    syncCreateButton.BorderColor = Colors.Highlight.Trans.Medium;
                    verifyLabel.IsVisible = true;
                    verify.IsVisible = true;
                    emailLabel.IsVisible = true;
                    email.IsVisible = true;
                }
                else
                {
                    syncLoginButton.BorderColor = Colors.Highlight.Trans.Medium;
                    syncCreateButton.BorderColor = Colors.Base.Trans.Low;
                    verifyLabel.IsVisible = false;
                    verify.IsVisible = false;
                    emailLabel.IsVisible = false;
                    email.IsVisible = false;
                }
                usernameErrors.IsVisible= false;
                passwordErrors.IsVisible = false;
                verifyErrors.IsVisible = false;
                emailErrors.IsVisible = false;
            }
        }
    }
}
