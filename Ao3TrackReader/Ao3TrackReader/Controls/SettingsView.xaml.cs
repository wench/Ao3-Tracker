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
            usernameErrors.IsVisible = false;
            passwordErrors.IsVisible = false;
            verifyErrors.IsVisible = false;
            emailErrors.IsVisible = false;

            string s_username = username.Text;
            string s_password = password.Text;
            string s_email = email.Text;
            string s_verify = verify.Text;

            Task.Run(async () =>
            {
                var errors = new Dictionary<string, string>();

                if (string.IsNullOrEmpty(s_username)) errors["username"] = "You must enter a username";
                if (string.IsNullOrEmpty(s_password)) errors["password"] = "You must enter a password";

                if (errors.Count == 0)
                {
                    if (isCreateUser)
                    {
                        if (s_password != s_verify)
                        {
                            errors["verify"] = "Passwords do not match";
                        }
                        else
                        {
                            errors = await App.Storage.UserCreate(s_username, s_password, s_email);
                        }
                    }
                    else
                    {
                        errors = await App.Storage.UserLogin(s_username, s_password);
                    }
                }
                wpv.DoOnMainThread(() => 
                { 
                    if (errors?.Count > 0)
                    {
                        foreach (var error in errors)
                        {
                            switch (error.Key)
                            {
                                case "username":
                                    usernameErrors.Text = error.Value;
                                    usernameErrors.IsVisible = true;
                                    break;

                                case "password":
                                    passwordErrors.Text = error.Value;
                                    passwordErrors.IsVisible = true;
                                    break;

                                case "verify":
                                    if (isCreateUser) break;
                                    verifyErrors.Text = error.Value;
                                    verifyErrors.IsVisible = true;
                                    break;

                                case "email":
                                    if (isCreateUser) break;
                                    emailErrors.Text = error.Value;
                                    emailErrors.IsVisible = true;
                                    break;
                            }
                        }
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
            Task.Run(async () =>
            {
                await App.Storage.UserLogout();
                wpv.DoOnMainThread(() =>
                {
                    UpdateSyncForm();
                });
            });
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
                text.Spans.Add(new Span { Text = name, ForegroundColor = Colors.Highlight.High });
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
