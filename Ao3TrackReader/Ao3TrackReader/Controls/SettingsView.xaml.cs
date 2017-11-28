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
using Ao3TrackReader.Models;
using Ver = Ao3TrackReader.Version.Version;

namespace Ao3TrackReader.Controls
{
    [Xamarin.Forms.Xaml.XamlCompilation(Xamarin.Forms.Xaml.XamlCompilationOptions.Skip)]
    public partial class SettingsView : PaneView
    {
        public static readonly BindableProperty DatabaseVariableProperty =
          BindableProperty.CreateAttached("DatabaseVariable", typeof(string), typeof(SettingsView), null);

        public static string GetDatabaseVariable(Element view)
        {
            return (string)view.GetValue(DatabaseVariableProperty);
        }

        public static void SetDatabaseVariable(Element view, string value)
        {
            view.SetValue(DatabaseVariableProperty, value);
        }

        bool isCreateUser;

        public SettingsView()
        {
            DescendantAdded += SettingsView_DescendantAdded;
            InitializeComponent();

            aboutText.TextEx = new Text.Span
            {
                Nodes = {
                    new Text.String { Text = "Archive Track Reader Version " + Ver.Major + "." + Ver.Minor + "." + Ver.Build },
                    new Text.Br(),
                    new Text.String { Text = Ver.Copyright },
                    new Text.Br(),
                    new Text.String { Text = "Source Code for this build available at: " },
                    new Text.Br(),
                    new Text.Link { Nodes = { Ver.Source.AbsoluteUri }, Foreground = Ao3TrackReader.Resources.Colors.Highlight, Href = Ver.Source },
                    new Text.Br(),
                    new Text.String { Text = "Under the terms of '" + Ver.License.Name + "': " },
                    new Text.Br(),
                    new Text.Link { Nodes = { Ver.License.uri.AbsoluteUri }, Foreground = Ao3TrackReader.Resources.Colors.Highlight, Href = Ver.License.uri },
                }
            };

            isCreateUser = false;
        }

        protected override void OnIsOnScreenChanging(bool newValue)
        {
            if (newValue == true)
            {
                UpdateSyncForm();
                sendErrorsSwitch.IsToggled = App.LogErrors;
                backButtonSetting.IsVisible = App.HaveOSBackButton;
                useBlurSetting.IsVisible = App.IsBlurSupported;
    
                UpdateListFilters();
                RefreshDatabaseVariableElems();
            }
        }

        public void OnSendErrorsSwitch(object sender, EventArgs e)
        {
            App.LogErrors = sendErrorsSwitch.IsToggled;
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


        private void UpdateSyncForm()
        {
            string name = App.Storage.Username;
            if (App.Storage.HaveCredentials && !string.IsNullOrWhiteSpace(name))
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
                    syncLoginButton.Style = Resources["TextButtonInactive"] as Style;
                    syncCreateButton.Style = Resources["TextButtonActive"] as Style;
                    verifyField.IsVisible = true;
                    emailField.IsVisible = true;
                }
                else
                {
                    syncCreateButton.Style = Resources["TextButtonInactive"] as Style;
                    syncLoginButton.Style = Resources["TextButtonActive"] as Style;
                    verifyField.IsVisible = false;
                    emailField.IsVisible = false;
                }
                usernameErrors.IsVisible = false;
                passwordErrors.IsVisible = false;
                verifyErrors.IsVisible = false;
                emailErrors.IsVisible = false;
            }
        }

        public async void OnSyncSubmit(object sender, EventArgs e)
        {
            syncSubmitButton.IsEnabled = false;
            syncIndicator.IsVisible = true;
            syncIndicator.Content = new ActivityIndicator();
            syncIndicator.Content.IsRunning = true;
            usernameErrors.IsVisible = false;
            passwordErrors.IsVisible = false;
            verifyErrors.IsVisible = false;
            emailErrors.IsVisible = false;

            string s_username = username.Text;
            string s_password = password.Text;
            string s_email = email.Text;
            string s_verify = verify.Text;

            string old_username = App.Storage.Username;

            var errors = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(s_username)) errors["username"] = "You must enter a username";
            if (string.IsNullOrWhiteSpace(s_password)) errors["password"] = "You must enter a password";

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
                        errors = await App.Storage.UserCreateAsync(s_username, s_password, s_email);
                    }
                }
                else
                {
                    errors = await App.Storage.UserLoginAsync(s_username, s_password);
                }
            }

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
                            if (!isCreateUser) break;
                            verifyErrors.Text = error.Value;
                            verifyErrors.IsVisible = true;
                            break;

                        case "email":
                            if (!isCreateUser) break;
                            emailErrors.Text = error.Value;
                            emailErrors.IsVisible = true;
                            break;
                    }
                }
            }
            else
            {
                UpdateSyncForm();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.WhenAll(
                        wvp.ReadingList.SyncToServerAsync(old_username != s_username),
                        Task.Run(async()=>
                        {
                            await Data.ListFiltering.Instance.SyncWithServerAsync(old_username != s_username);
                            if (IsOnScreen)
                            {
                                await wvp.DoOnMainThreadAsync(async () => await UpdateListFiltersAsync());
                            }
                        })
                    );
                }, TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning).ConfigureAwait(false);
            }
            await wvp.DoOnMainThreadAsync(() =>
            {
                syncSubmitButton.IsEnabled = true;
                syncIndicator.Content = null;
                syncIndicator.IsVisible = false;
            });
        }

        public async void OnSyncLogout(object sender, EventArgs e)
        {
            await App.Storage.UserLogoutAsync();
            UpdateSyncForm();
        }

        void UpdateListFilters()
        {
            UpdateListFiltersAsync().ConfigureAwait(false);
        }

        async Task UpdateListFiltersAsync()
        {
            await wvp.DoOnMainThreadAsync(() =>
            {
                listFilterTags.IsEnabled = false;
                listFilterAuthors.IsEnabled = false;
                listFilterWorks.IsEnabled = false;
                listFilterSerieses.IsEnabled = false;
                listFilterSave.IsEnabled = false;
                listFilterIndicator.IsVisible = true;
                if (listFilterIndicator.Content == null)
                {
                    listFilterIndicator.Content = new ActivityIndicator();
                    listFilterIndicator.Content.IsRunning = true;
                }
            });

            await Task.Run(async () =>
            {
                Data.ListFiltering.Instance.GetFilterStrings(out var tags, out var authors, out var works, out var serieses);

                await wvp.DoOnMainThreadAsync(() =>
                {
                    listFilterTags.Text = tags;
                    listFilterAuthors.Text = authors;
                    listFilterWorks.Text = works;
                    listFilterSerieses.Text = serieses;

                    listFilterTags.IsEnabled = true;
                    listFilterAuthors.IsEnabled = true;
                    listFilterWorks.IsEnabled = true;
                    listFilterSerieses.IsEnabled = true;
                    listFilterSave.IsEnabled = true;
                    listFilterIndicator.Content = null;
                    listFilterIndicator.IsVisible = false;
                });
            });
        }

        void OnSaveFilters(object sender, EventArgs e)
        {
            OnSaveFiltersAsync().ConfigureAwait(false);
        }

        async Task OnSaveFiltersAsync()
        {
            await wvp.DoOnMainThreadAsync(() =>
            {
                listFilterTags.IsEnabled = false;
                listFilterAuthors.IsEnabled = false;
                listFilterWorks.IsEnabled = false;
                listFilterSerieses.IsEnabled = false;
                listFilterSave.IsEnabled = false;
                listFilterIndicator.IsVisible = true;
                if (listFilterIndicator.Content == null)
                {
                    listFilterIndicator.Content = new ActivityIndicator();
                    listFilterIndicator.Content.IsRunning = true;
                }
            });

            await Data.ListFiltering.Instance.SetFilterStringsAsync(listFilterTags.Text, listFilterAuthors.Text, listFilterWorks.Text, listFilterSerieses.Text);

            await UpdateListFiltersAsync();
        }       

        void DatabaseVariableElem_Changed(object sender, EventArgs e)
        {
            if (!IsOnScreen) return;
            Element elem = (Element)sender;

            string varname = GetDatabaseVariable(elem);
            if (string.IsNullOrWhiteSpace(varname)) return;

            if (elem is Switch sw)
            {
                App.Database.SaveVariable(varname, sw.IsToggled);
            }
            else if (elem is DropDown dropdown)
            {
                App.Database.SaveVariable(varname, (dropdown.SelectedItem as IKeyedItem).Key);
            }
        }

        void RefreshDatabaseVariableElem(Element elem)
        {
            string varname = GetDatabaseVariable(elem);
            if (string.IsNullOrWhiteSpace(varname)) return;

            if (elem is Switch sw)
            {
                App.Database.TryGetVariable(varname, bool.TryParse, out bool b);
                sw.IsToggled = b;
            }
            else if (elem is DropDown dropdown)
            {
                if (dropdown.ItemsSource is IKeyedItemList itemlist)
                {
                    string str = App.Database.GetVariable(varname);
                    IKeyedItem item = itemlist.Lookup(str);
                    dropdown.SelectedItem = item;
                }
            }
        }

        HashSet<Element> dbVarElements = new HashSet<Element>();

        private void SettingsView_DescendantAdded(object sender, ElementEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(GetDatabaseVariable(e.Element)))
            {
                dbVarElements.Add(e.Element);
                if (e.Element is Switch sw) sw.Toggled += DatabaseVariableElem_Changed;
                else if (e.Element is DropDown dd) dd.SelectedIndexChanged += DatabaseVariableElem_Changed;
            }
        }

        private void RefreshDatabaseVariableElems()
        {
            foreach (var elem in dbVarElements)
                RefreshDatabaseVariableElem(elem);
        }
    }
}
