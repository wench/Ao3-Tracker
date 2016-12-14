using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
    public partial class SettingsView : ContentView
    {
        public static Color GroupTitleColor
        {
            get
            {
                var c = App.Colors["SystemChromeAltLowColor"];
                return new Color(((int)(c.R * 255) ^ 0x90) / 255.0, ((int)(c.G * 255) ^ 0) / 510.0, ((int)(c.B * 255) ^ 0) / 255.0);
            }
        }

        public static Color ButtonActiveColor
        {
            get
            {
                var c = App.Colors["SystemBaseMediumColor"];
                return new Color(((int)(c.R * 255) ^ 0x90) / 255.0, ((int)(c.G * 255) ^ 0) / 510.0, ((int)(c.B * 255) ^ 0) / 255.0, c.A);
            }
        }
        public static Color ButtonDefaultColor
        {
            get 
            {
                return App.Colors["SystemBaseLowColor"];
            }
}

        bool isCreateUser;
        WebViewPage wpv;
        Dictionary<string, string> themes;

        public SettingsView (WebViewPage wpv)
        {
            this.wpv = wpv;
            InitializeComponent();

            var c = App.Colors["SystemAltMediumHighColor"];
            BackgroundColor = new Color(c.R, c.G, c.B, (3 + c.A) / 4);
            isCreateUser = false;
            TranslationX = 480;

            themes = new Dictionary<string, string> {
                { "light", "Light" },
                { "dark", "Dark" }
            };
            themeList.ItemsSource = themes.Values;
        }

        public bool IsOnScreen
        {
            get
            {
                return TranslationX < Width / 2;
            }
            set
            {
                if (value == false)
                {
                    ViewExtensions.CancelAnimations(this);
                    this.TranslateTo(Width, 0, 100, Easing.CubicIn);
                }
                else
                {
                    ViewExtensions.CancelAnimations(this);
                    this.TranslateTo(0, 0, 100, Easing.CubicIn);
                    UpdateSyncForm();
                    SelectCurrentTheme();
                }
            }
        }

        public void OnClose(object sender, EventArgs e)
        {
            IsOnScreen = false;
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
                text.Spans.Add(new Span { Text = name, ForegroundColor = GroupTitleColor });
                syncLoggedInLabel.FormattedText = text;
            }
            else
            {
                syncLoggedIn.IsVisible = false;
                syncForm.IsVisible = true;
                if (isCreateUser)
                {
                    syncLoginButton.BorderColor = ButtonDefaultColor;
                    syncCreateButton.BorderColor = ButtonActiveColor;
                    verifyLabel.IsVisible = true;
                    verify.IsVisible = true;
                    emailLabel.IsVisible = true;
                    email.IsVisible = true;
                }
                else
                {
                    syncLoginButton.BorderColor = ButtonActiveColor;
                    syncCreateButton.BorderColor = ButtonDefaultColor;
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
