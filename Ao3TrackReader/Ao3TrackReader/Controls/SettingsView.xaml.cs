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
    public partial class SettingsView : PaneView
    {
        bool isCreateUser;

        KeyedItem<string> defaultTheme;

        public SettingsView()
        {
            InitializeComponent();

            aboutText.TextEx = new Text.Span
            {
                Nodes = {
                    new Text.String { Text = "Ao3Track Reader Version " + Ver.Major + "." + Ver.Minor + "." + Ver.Build },
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

            themes.Add(defaultTheme = new KeyedItem<string>("light", "Light"));
            themes.Add(new KeyedItem<string>("dark", "Dark"));
        }

        protected override void OnIsOnScreenChanging(bool newValue)
        {
            if (newValue == true)
            {
                UpdateSyncForm();
                SelectCurrentTheme();
                SelectBackButtonMode();
                httpsSwitch.IsToggled = Data.Ao3SiteDataLookup.UseHttps;
                sendErrorsSwitch.IsToggled = App.LogErrors;
                UpdateUnitConvs();
                UpdateNavOptions();
                UpdateFontSizeUI();
                UpdateListFilters();
                UpdateSummaryDisplay();
            }
        }

        public void OnSendErrorsSwitch(object sender, EventArgs e)
        {
            App.LogErrors = sendErrorsSwitch.IsToggled;
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
            syncIndicator.Content = new ActivityIndicator();
            syncIndicator.IsVisible = true;
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

        void SelectCurrentTheme()
        {
            string theme = Ao3TrackReader.App.Database.GetVariable("Theme");
            var item = themes.Find(theme) ?? defaultTheme;
            App.Database.SaveVariable("Theme", item.Key);
            themeDropDown.SelectedItem = item;
        }

        void OnThemeSelected(object sender, EventArgs e)
        {
            if (!IsOnScreen) return;

            var item = themeDropDown.SelectedItem as KeyedItem<string>;
            if (item == null)
            {
                SelectCurrentTheme();
                return;
            }
            App.Database.SaveVariable("Theme", item.Key);
        }

        void SelectBackButtonMode()
        {
            if (!App.HaveOSBackButton)
            {
                backButtonForm.IsVisible = false;
                return;
            }
            backButtonForm.IsVisible = true;

            bool? show = null;
            App.Database.TryGetVariable("ShowBackButton", bool.TryParse, out show);

            var item = backButtonMode.Find(show) ?? backButtonMode[0];
            App.Database.SaveVariable("ShowBackButton", item.Key);
            backButtonModeDropDown.SelectedItem = item;
        }

        void OnBackButtonModeSelected(object sender, EventArgs e)
        {
            if (!App.HaveOSBackButton || !IsOnScreen) return;

            var item = backButtonModeDropDown.SelectedItem as KeyedItem<bool?>;
            if (item != null)
            {
                App.Database.SaveVariable("ShowBackButton", item.Key);
                wvp.UpdateBackButton();
            }
        }

        private void UpdateUnitConvs()
        {
            bool? v;

            App.Database.TryGetVariable("UnitConvOptions.tempToC", bool.TryParse, out v);
            unitConvTempDropDown.SelectedItem = unitConvModeTemp.Find(v);

            App.Database.TryGetVariable("UnitConvOptions.distToM", bool.TryParse, out v);
            unitConvDistDropDown.SelectedItem = unitConvMode.Find(v);

            App.Database.TryGetVariable("UnitConvOptions.volumeToM", bool.TryParse, out v);
            unitConvVolumeDropDown.SelectedItem = unitConvMode.Find(v);

            App.Database.TryGetVariable("UnitConvOptions.weightToM", bool.TryParse, out v);
            unitConvWeightDropDown.SelectedItem = unitConvMode.Find(v);
        }

        void OnUnitConvSelected(object sender, EventArgs e)
        {
            if (!IsOnScreen) return;

            string varname;
            if (sender == unitConvTempDropDown) varname = "UnitConvOptions.tempToC";
            else if (sender == unitConvDistDropDown) varname = "UnitConvOptions.distToM";
            else if (sender == unitConvVolumeDropDown) varname = "UnitConvOptions.volumeToM";
            else if (sender == unitConvWeightDropDown) varname = "UnitConvOptions.weightToM";
            else return;

            var dropdown = sender as DropDown;
            var val = (KeyedItem<bool?>)dropdown.SelectedItem;

            App.Database.SaveVariable(varname, val.Key);
        }

        private ref NavigateBehaviour UnitConvForControl(DropDown control, out string name)
        {
            if (control == toolbarBackDropDown)
            {
                name = "ToolbarBackBehaviour";
                return ref wvp.ToolbarBackBehaviour;
            }

            if (control == toolbarForwardDropDown)
            {
                name = "ToolbarForwardBehaviour";
                return ref wvp.ToolbarForwardBehaviour;
            }

            if (control == swipeBackDropDown)
            {
                name = "SwipeBackBehaviour";
                return ref wvp.SwipeBackBehaviour;
            }

            if (control == swipeFowardDropDown)
            {
                name = "SwipeForwardBehaviour";
                return ref wvp.SwipeForwardBehaviour;
            }

            throw new ArgumentException("Invalid control name", nameof(control));
        }
        private void UpdateNavOptions()
        {
            foreach (var dropdown in navigationForm.FindChildren<DropDown>())
            {
                ref NavigateBehaviour value = ref UnitConvForControl(dropdown, out var dbname);
                dropdown.SelectedItem = navigateMode.Find(value);
            }
        }

        void OnNavOptionSelected(object sender, EventArgs e)
        {
            if (!IsOnScreen) return;

            var dropdown = sender as DropDown;
            ref NavigateBehaviour value = ref UnitConvForControl(dropdown, out var dbname);

            value = (dropdown.SelectedItem as KeyedItem<NavigateBehaviour>).Key;
            App.Database.SaveVariable(dbname, value);
        }

        void UpdateFontSizeUI()
        {
            App.Database.TryGetVariable("LogFontSizeUI", int.TryParse, out int LogFontSizeUI, 0);
            fontSizeUIDropDown.SelectedItem = fontSizeUIList.Find(LogFontSizeUI);
        }

        void OnFontSizeUISelected(object sender, EventArgs e)
        {
            if (!IsOnScreen) return;

            App.Database.SaveVariable("LogFontSizeUI", (fontSizeUIDropDown.SelectedItem as KeyedItem<int>).Key);
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
                if (listFilterIndicator.Content == null) listFilterIndicator.Content = new ActivityIndicator();
                listFilterIndicator.IsVisible = true;
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
                if (listFilterIndicator.Content == null) listFilterIndicator.Content = new ActivityIndicator();
                listFilterIndicator.IsVisible = true;
            });

            await Data.ListFiltering.Instance.SetFilterStringsAsync(listFilterTags.Text, listFilterAuthors.Text, listFilterWorks.Text, listFilterSerieses.Text);

            await UpdateListFiltersAsync();
        }

        private void UpdateSummaryDisplay()
        {
            bool b;

            App.Database.TryGetVariable("TagOptions.showCatTags", bool.TryParse, out b);
            sumShowCatTags.IsToggled = b;

            App.Database.TryGetVariable("TagOptions.showWIPTags", bool.TryParse, out b);
            sumShowWIPTags.IsToggled = b;

            App.Database.TryGetVariable("TagOptions.showRatingTags", bool.TryParse, out b);
            sumShowRatingTags.IsToggled = b;

            App.Database.TryGetVariable("ReadingList.showTagsDefault", bool.TryParse, out b);
            sumShowRLTagsDef.IsToggled = b;

            App.Database.TryGetVariable("ReadingList.showCompleteDefault", bool.TryParse, out b);
            sumShowRLCompleteDef.IsToggled = b;
        }

        void OnSummaryDisplayChanged(object sender, EventArgs e)
        {
            if (!IsOnScreen) return;

            string varname;
            if (sender == sumShowCatTags) varname = "TagOptions.showCatTags";
            else if (sender == sumShowWIPTags) varname = "TagOptions.showWIPTags";
            else if (sender == sumShowRatingTags) varname = "TagOptions.showRatingTags";
            else if (sender == sumShowRLTagsDef) varname = "ReadingList.showTagsDefault";
            else if (sender == sumShowRLCompleteDef) varname = "ReadingList.showCompleteDefault";
            else return;

            var sw = sender as Switch;
           
            App.Database.SaveVariable(varname, sw.IsToggled);
        }
    }
}
