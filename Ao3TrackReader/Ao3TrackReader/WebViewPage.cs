using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Ao3TrackReader.Helper;

#if WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;
#endif


namespace Ao3TrackReader
{
    public partial class WebViewPage : ContentPage, IEventHandler
    {
        public WebViewPage()
        {
            Title = "Ao3Track Reader";
            NavigationPage.SetHasNavigationBar(this, true);

            var layout = new StackLayout {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Spacing = 0
            };

            var wv = CreateWebView();
            wv.VerticalOptions = LayoutOptions.FillAndExpand;
            wv.HorizontalOptions = LayoutOptions.FillAndExpand;
            layout.Children.Add(wv);
            Navigate("https://archiveofourown.com/");


            var commandBar = CreateCommandBar();
#if WINDOWS_UWP
            commandBar.PrimaryCommands.Add(jumpButton = CreateAppBarButton("Jump", new Windows.UI.Xaml.Controls.SymbolIcon(Windows.UI.Xaml.Controls.Symbol.ShowBcc), false, this.OnJumpClicked));
#else
#endif
            layout.Children.Add(commandBar);

            /*
            ToolbarItem tbi = null;

            tbi = new ToolbarItem("Jump", "jump.png", () =>
            {
                webView.OnJumpClicked();
            }, 0, 0);

            ToolbarItems.Add(tbi);
            */
            Content = layout;

        }

        static object locker = new object();

        public IDictionary<long, IWorkChapter> GetWorkChapters(long[] works)
        {
            // Get locals
            IDictionary<long, IWorkChapter> result = new Dictionary<long, IWorkChapter>();

            // Make this behave like the Web Extension. 
            // If syncing wait till it's finished

            foreach (var item in App.Database.GetItems(works))
            {
                result[item.id] = new WorkChapter { number = item.number, chapterid = item.chapterid, location = item.location };
            }

            return result;
        }

        public void SetWorkChapters(IDictionary<long, IWorkChapter> works)
        {
            long now = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000;

            // Set Locals
            List<Work> items = new List<Work>(works.Count);
            foreach (var work in works)
            {
                items.Add(new Ao3TrackReader.Work { id = work.Key, chapterid = work.Value.chapterid, number = work.Value.number, location = work.Value.location, timestamp = now });
            }

            var changes = App.Database.SaveItems(items);

            // Send changes to server

        }

        public void OnJumpClicked()
        {
            Task.Run(() =>
            {
                helper?.JumpToLastLocation(false);
            });
        }

        public void EnableJumpToLastLocation(bool enable)
        {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
#if WINDOWS_UWP
                if (jumpButton != null) jumpButton.IsEnabled = enable;
#else
                
#endif
            });
        }

    }
}
