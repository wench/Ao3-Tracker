using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Ao3TrackReader.Helper;
using System.Threading;
using System.Text.RegularExpressions;

#if WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;
using SymbolIcon = Windows.UI.Xaml.Controls.SymbolIcon;
using Symbol = Windows.UI.Xaml.Controls.Symbol;
using AppBarButton = Windows.UI.Xaml.Controls.AppBarButton;
#endif


namespace Ao3TrackReader
{
    public partial class WebViewPage : ContentPage, IEventHandler
    {
#if WINDOWS_UWP
        AppBarButton jumpButton { get; set; }
        AppBarButton incFontSizeButton { get; set; }
        AppBarButton decFontSizeButton { get; set; }
        AppBarButton nextPageButton { get; set; }
        AppBarButton prevPageButton { get; set; }
        public Windows.UI.Core.CoreDispatcher Dispatcher { get; private set; }
#endif
        Label PrevPageIndicator;
        Label NextPageIndicator;
        GroupList<Models.Ao3PageViewModel> readingListBacking;
        Controls.ReadingListView readingList;
        Entry urlEntry;
        StackLayout urlBar;
        AbsoluteLayout modelPopup;

        public WebViewPage()
        {
            Title = "Ao3Track Reader";
            NavigationPage.SetHasNavigationBar(this, true);


            var mainlayout = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Spacing = 0
            };

            Dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;

            var commandBar = CreateCommandBar();
#if WINDOWS_UWP
            commandBar.PrimaryCommands.Add(prevPageButton = CreateAppBarButton("Back", new SymbolIcon(Symbol.Back), false, () => { GoBack(); }));
            commandBar.PrimaryCommands.Add(nextPageButton = CreateAppBarButton("Forward", new SymbolIcon(Symbol.Forward), false, () => { GoForward(); }));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Refresh", new SymbolIcon(Symbol.Refresh), true, () => WebView.Refresh()));
            commandBar.PrimaryCommands.Add(jumpButton = CreateAppBarButton("Jump", new SymbolIcon(Symbol.ShowBcc), false, this.OnJumpClicked));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Reading List", new SymbolIcon(Symbol.Bookmarks), true, () =>
            {
                if (readingList.TranslationX < 240)
                {
                    readingList.Unfocus();
                }
                else
                {
                    readingList.TranslateTo(0, 0, 100, Easing.CubicIn);
                    readingList.Focus();
                }
            }));
            commandBar.PrimaryCommands.Add(incFontSizeButton = CreateAppBarButton("Font Increase", new SymbolIcon(Symbol.FontIncrease), true, () =>
                FontSize += 10));
            commandBar.PrimaryCommands.Add(decFontSizeButton = CreateAppBarButton("Font Decrease", new SymbolIcon(Symbol.FontDecrease), true, () =>
                FontSize -= 10));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Sync", new SymbolIcon(Symbol.Sync), true, () => { }));
            commandBar.PrimaryCommands.Add(CreateAppBarButton("Url Bar", new SymbolIcon(Symbol.Rename), true, () =>
            {
                if (urlBar.IsVisible)
                {
                    urlBar.IsVisible = false;
                    urlBar.Unfocus();
                }
                else
                {
                    urlEntry.Text = WebView.Source.ToString();
                    urlBar.IsVisible = true;
                    urlEntry.Focus();
                }
            }));

            commandBar.SecondaryCommands.Add(CreateAppBarButton("Reset Font Size", new SymbolIcon(Symbol.Font), true, () => FontSize = 100));
            commandBar.SecondaryCommands.Add(CreateAppBarButton("Settings", new SymbolIcon(Symbol.Setting), true, SettingsButton_Clicked));

#else
#endif
            NextPageIndicator = new Label { Text = "Next Page", Rotation = 90, VerticalTextAlignment = TextAlignment.Start, HorizontalTextAlignment = TextAlignment.Center, IsVisible = false };
            AbsoluteLayout.SetLayoutBounds(NextPageIndicator, new Rectangle(.98, .5, 100, 100));
            AbsoluteLayout.SetLayoutFlags(NextPageIndicator, AbsoluteLayoutFlags.PositionProportional);

            PrevPageIndicator = new Label { Text = "Previous Page", Rotation = 270, VerticalTextAlignment = TextAlignment.Start, HorizontalTextAlignment = TextAlignment.Center, IsVisible = false };
            AbsoluteLayout.SetLayoutBounds(PrevPageIndicator, new Rectangle(.02, .5, 100, 100));
            AbsoluteLayout.SetLayoutFlags(PrevPageIndicator, AbsoluteLayoutFlags.PositionProportional);

            var wv = CreateWebView();
            AbsoluteLayout.SetLayoutBounds(wv, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(wv, AbsoluteLayoutFlags.All);
            Navigate("https://archiveofourown.com/");

            // retore font size!
            FontSize = 100;

            readingListBacking = new GroupList<Models.Ao3PageViewModel>();

            readingList = new Controls.ReadingListView();
            AbsoluteLayout.SetLayoutBounds(readingList, new Rectangle(1, 0, 480, 1));
            AbsoluteLayout.SetLayoutFlags(readingList, AbsoluteLayoutFlags.HeightProportional | AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.YProportional);
            readingList.ItemsSource = readingListBacking;
            readingList.BackgroundColor = App.Colors["SystemAltMediumHighColor"];
            readingList.TranslationX = 480;
            readingList.Unfocused += ReadingList_Unfocused;
            readingList.ItemTapped += ReadingList_ItemTapped;

            // Restore the reading list contents!
            var items = new Dictionary<string, Models.ReadingList>();
            foreach (var i in App.Database.GetReadingListItems())
            {
                items[i.Uri] = i;
            }

            if (items.Count == 0) items.Add("https://archiveofourown.org/", new Models.ReadingList());

            var models = Data.Ao3SiteDataLookup.LookupQuick(items.Keys);
            foreach (var m in models)
            {
                if (m.Value != null)
                {
                    var item = items[m.Key];
                    if (string.IsNullOrWhiteSpace(m.Value.Title))
                        m.Value.Title = item.Title;
                    if (string.IsNullOrWhiteSpace(m.Value.PrimaryTag) || m.Value.PrimaryTag=="<Work>")
                        m.Value.PrimaryTag = item.PrimaryTag;

                    var viewmodel = new Models.Ao3PageViewModel { BaseData = m.Value };
                    readingListBacking.Add(viewmodel);
                    refreshReadingListItem(viewmodel);
                }
            }

            urlBar = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Spacing = 4
            };
            //AbsoluteLayout.SetLayoutBounds(urlFrame, new Rectangle(0, 1, 1, 44));
            //AbsoluteLayout.SetLayoutFlags(urlFrame, AbsoluteLayoutFlags.WidthProportional | AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.YProportional);
            urlEntry = new Entry
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            urlEntry.Completed += UrlButton_Clicked;
            urlEntry.Keyboard = Keyboard.Url;

            urlBar.Children.Add(urlEntry);

            var urlButton = new Button()
            {
                Text = "Go",
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };
            urlButton.Clicked += UrlButton_Clicked;

            var urlCancel = new Button()
            {
                Text = "Close",
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };
            urlCancel.Clicked += UrlCancel_Clicked;

            urlBar.Children.Add(urlEntry);
            urlBar.Children.Add(urlButton);
            urlBar.Children.Add(urlCancel);
            urlBar.BackgroundColor = Color.Black;
            urlBar.IsVisible = false;

            mainlayout.Children.Add(new AbsoluteLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = {
                        wv,
                        PrevPageIndicator,
                        NextPageIndicator,
                        readingList,
                    }
            });
            mainlayout.Children.Add(urlBar);
            mainlayout.Children.Add(commandBar);

            /*
            ToolbarItem tbi = null;

            tbi = new ToolbarItem("Jump", "jump.png", () =>
            {
                webView.OnJumpClicked();
            }, 0, 0);

            ToolbarItems.Add(tbi);
            */
            AbsoluteLayout.SetLayoutBounds(mainlayout, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(mainlayout, AbsoluteLayoutFlags.All);

            modelPopup = new AbsoluteLayout();
            AbsoluteLayout.SetLayoutBounds(modelPopup, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(modelPopup, AbsoluteLayoutFlags.All);
            //modelPopup.BackgroundColor = new Color(0, 0, 0, .5);

            var outerlayout = new AbsoluteLayout
            {
                Children =
                {
                    mainlayout,
                    modelPopup

                }

            };

            Content = outerlayout;
        }

        private void ReadingList_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var item = e.Item as Models.Ao3PageViewModel;
            if (item != null) Navigate(item.Uri.AbsoluteUri);
        }

        private void UrlCancel_Clicked(object sender, EventArgs e)
        {
            urlBar.IsVisible = false;
            urlBar.Unfocus();
        }

        private async void UrlButton_Clicked(object sender, EventArgs e)
        {
            urlBar.IsVisible = false;
            urlBar.Unfocus();
            try
            {
                var uri = new UriBuilder(urlEntry.Text);
                if (uri.Host == "archiveofourown.org" || uri.Host == "www.archiveofourown.org")
                {
                    if (uri.Scheme == "http")
                    {
                        uri.Scheme = "https";
                    }
                    uri.Port = -1;
                    WebView.Navigate(uri.Uri);
                }
                else
                {
                    await DisplayAlert("Url error", "Can only enter urls on archiveofourown.org", "Ok");
                }
            }
            catch
            {

            }

        }

        private void ReadingList_Unfocused(object sender, FocusEventArgs e)
        {
            readingList.TranslateTo(480, 0, 100, Easing.CubicIn);
        }

        public int FontSizeMax { get { return 300; } }
        public int FontSizeMin { get { return 10; } }
        private int font_size = 100;
        public int FontSize
        {
            get { return font_size; }
            set
            {
                font_size = value;
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    decFontSizeButton.IsEnabled = FontSize > FontSizeMin;
                    incFontSizeButton.IsEnabled = FontSize < FontSizeMax;
                });
                helper.OnAlterFontSizeEvent();
            }
        }


        static object locker = new object();

#if WINDOWS_UWP
        public Windows.Foundation.IAsyncOperation<IDictionary<long, IWorkChapter>> GetWorkChaptersAsync(long[] works)
        {
            return App.Storage.getWorkChaptersAsync(works).AsAsyncOperation();
        }
#else
        public Windows.Foundation.IAsyncOperation<IDictionary<long, IWorkChapter>> GetWorkChaptersAsync(long[] works)
        {
            return App.Storage.getWorkChaptersAsync(works);
        }
#endif

        public object DoOnMainThread(MainThreadFunc function)
        {

            if (Dispatcher.HasThreadAccess)
            {
                return function();
            }
            else
            {
                object result = null;
                ManualResetEventSlim handle = new ManualResetEventSlim();

                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    result = function();
                    handle.Set();
                });
                handle.Wait();

                return result;
            }
        }

        public void DoOnMainThread(MainThreadAction function)
        {
            if (Dispatcher.HasThreadAccess)
            {
                function();
            }
            else
            {
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    function();
                });
            }
        }


        public void SetWorkChapters(IDictionary<long, IWorkChapter> works)
        {
            App.Storage.setWorkChapters(works);
        }

        public void OnJumpClicked()
        {
            Task.Run(() =>
            {
                helper.OnJumpToLastLocation(false);
            });
        }

        public bool JumpToLastLocationEnabled
        {
            set
            {
#if WINDOWS_UWP
                if (jumpButton != null) jumpButton.IsEnabled = value;
#else                
#endif
            }
            get
            {
#if WINDOWS_UWP
                return jumpButton?.IsEnabled ?? false;
#else
#endif              
            }
        }

        public async void SettingsButton_Clicked()
        {
            var settingsPage = new SettingsPage();

            await Navigation.PushModalAsync(settingsPage);
        }

        public bool showPrevPageIndicator
        {
            get { return PrevPageIndicator.IsVisible; }
            set
            {
                PrevPageIndicator.IsVisible = value;
            }
        }
        public bool showNextPageIndicator
        {
            get { return NextPageIndicator.IsVisible; }
            set
            {
                NextPageIndicator.IsVisible = value;
            }
        }

        Regex regexPageQuery = new Regex(@"(?<PAGE>&?page=\d+&?)");
        public void addToReadingList(string href)
        {
            href = regexPageQuery.Replace(href, (m) => {
                if (m.Value.StartsWith("&") && m.Value.EndsWith("&")) return "&";
                else return "";
            });

            var uribuilder = new UriBuilder(href.TrimEnd('?'));

            if (uribuilder.Host == "archiveofourown.org" || uribuilder.Host == "www.archiveofourown.org")
            {
                if (uribuilder.Scheme == "http")
                {
                    uribuilder.Scheme = "https";
                }
                uribuilder.Port = -1;
            }
            else
                return;

            href = uribuilder.ToString();

            if (readingListBacking.Find((m) => m.Uri.AbsoluteUri == href) != null)
                return;

            var models = Data.Ao3SiteDataLookup.LookupQuick(new[] { href });
            var model = models.SingleOrDefault();
            if (model.Value == null) return;

            var viewmodel = new Models.Ao3PageViewModel { BaseData = model.Value };
            readingListBacking.Add(viewmodel);
            App.Database.SaveReadingListItems(new Models.ReadingList { Uri = model.Value.Uri.AbsoluteUri, PrimaryTag = model.Value.PrimaryTag, Title = model.Value.Title });
            refreshReadingListItem(viewmodel);
        }

        public void refreshReadingListItem(Models.Ao3PageViewModel viewmodel)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                var models = await Data.Ao3SiteDataLookup.LookupAsync(new[] { viewmodel.Uri.AbsoluteUri });

                DoOnMainThread(() =>
                {
                    var model = models.SingleOrDefault();
                    if (model.Value != null)
                    {
                        viewmodel.BaseData = model.Value;
                        App.Database.SaveReadingListItems(new Models.ReadingList { Uri = model.Value.Uri.AbsoluteUri, PrimaryTag = model.Value.PrimaryTag, Title = model.Value.Title });
                    }
                });
            });

        }
    }
}
