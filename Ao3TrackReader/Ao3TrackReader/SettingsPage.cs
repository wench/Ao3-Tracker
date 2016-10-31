using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Ao3TrackReader
{
    class SettingsPage : ContentPage
    {
        Entry username;
        Entry password;

        public SettingsPage()
        {
            Title = "Settings";
            NavigationPage.SetHasNavigationBar(this, true);

            var cancelButton = new Button { Text = "Cancel" };
            cancelButton.Clicked += CancelButton_Clicked;

            var loginButton = new Button { Text = "Login" };
            loginButton.Clicked += LoginButton_Clicked;

            var labelFontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label));

            Content = new StackLayout
            {
                Spacing = 16,
                Children =
                {
                    new StackLayout {
                        Orientation = StackOrientation.Horizontal,
                        Spacing = 16,
                        Children = {
                            new Label { Text = "Username", FontSize = labelFontSize },
                            (username = new Entry { Placeholder = "Enter your username", HorizontalOptions = LayoutOptions.FillAndExpand })
                        }
                    },
                    new StackLayout {
                        Spacing = 16,
                        Orientation = StackOrientation.Horizontal,                       
                        Children = {                            

                            new Label { Text = "Password", FontSize = labelFontSize },
                            (password = new Entry { Placeholder = "Enter your password", IsPassword = true, HorizontalOptions = LayoutOptions.FillAndExpand } )
                        }
                    },
                    new StackLayout {
                        Spacing = 16,
                        Orientation = StackOrientation.Horizontal,
                        Children = { cancelButton, loginButton }
                    }
                }
            };

        }

        private async void LoginButton_Clicked(object sender, EventArgs e)
        {
            Title = "Logging in...";
            IsEnabled = false;

            var errors = await App.Storage.Login(username.Text, password.Text);
            await Navigation.PopModalAsync();        
        }

        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
