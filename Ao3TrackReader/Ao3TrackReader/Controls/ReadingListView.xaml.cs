using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
    public class RLMenuItem : MenuItem
    {
        public event Func<RLMenuItem, bool> Filter;

        public bool OnFilter()
        {
            return Filter?.Invoke(this) ?? true;
        }
    }

    public partial class ReadingListView : StackLayout
    {
        GroupList<Models.Ao3PageViewModel> readingListBacking;
        private readonly WebViewPage wpv;

        public static Color GroupTitleColor
        {
            get
            {
                var c = App.Colors["SystemChromeAltLowColor"];
                return new Color(((int)(c.R * 255) ^ 0x90) / 255.0, ((int)(c.G * 255) ^ 0) / 510.0, ((int)(c.B * 255) ^ 0) / 255.0);
            }
        }
        public static Color GroupTypeColor { get { return App.Colors["SystemChromeHighColor"]; } }

        public static Color ItemBackgroundColor { get { return App.Colors["SystemListLowColor"]; } }

        double old_width;
        System.Collections.Concurrent.ConcurrentBag<Tuple<Models.Ao3PageViewModel, Models.Ao3PageModel>> updateBag;

        public ReadingListView(WebViewPage wpv)
        {
            this.wpv = wpv;

            InitializeComponent();

            updateBag = new System.Collections.Concurrent.ConcurrentBag<Tuple<Models.Ao3PageViewModel, Models.Ao3PageModel>>();
            TranslationX = old_width = 480;
            WidthRequest = old_width;
            readingListBacking = new GroupList<Models.Ao3PageViewModel>();
            var c = App.Colors["SystemAltMediumHighColor"];
            BackgroundColor = new Color(c.R, c.G, c.B, (3 + c.A) / 4);

            // Restore the reading list contents!
            var items = new Dictionary<string, Models.ReadingList>();
            foreach (var i in App.Database.GetReadingListItems())
            {
                items[i.Uri] = i;
            }

            List<Task> tasks = new List<Task>();
            if (items.Count == 0)
            {
                tasks.Add(AddAsync("https://archiveofourown.org/"));
            }
            else
            {
                var models = Data.Ao3SiteDataLookup.LookupQuick(items.Keys);
                foreach (var m in models)
                {
                    if (m.Value != null)
                    {
                        var item = items[m.Key];
                        if (string.IsNullOrWhiteSpace(m.Value.Title) || m.Value.Type == Models.Ao3PageType.Work)
                            m.Value.Title = item.Title;
                        if (string.IsNullOrWhiteSpace(m.Value.PrimaryTag) || m.Value.PrimaryTag == "<Work>")
                        {
                            m.Value.PrimaryTag = item.PrimaryTag;
                            var tagdata = Data.Ao3SiteDataLookup.LookupTagQuick(item.PrimaryTag);
                            if (tagdata != null) m.Value.PrimaryTagType = Data.Ao3SiteDataLookup.GetTypeForCategory(tagdata.category);
                            else m.Value.PrimaryTagType = Models.Ao3TagType.Other;
                        }

                        var viewmodel = new Models.Ao3PageViewModel { BaseData = m.Value };
                        readingListBacking.Add(viewmodel);
                        tasks.Add(RefreshAsync(viewmodel));
                    }
                }
            }
            ListView.ItemsSource = readingListBacking;
            Task.Run(async () =>
            {
                await Task.WhenAll(tasks);

                wpv.DoOnMainThread(() =>
                {
                    RefreshButton.IsEnabled = true;
                });
            });
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
                    ListView.Unfocus();
                    ViewExtensions.CancelAnimations(this);
                    this.TranslateTo(Width, 0, 100, Easing.CubicIn);
                }
                else
                {
                    ViewExtensions.CancelAnimations(this);
                    this.TranslateTo(0, 0, 100, Easing.CubicIn);
                    ListView.Focus();
                }
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            bool wasshowing = TranslationX < old_width / 2;
            old_width = width;

            base.OnSizeAllocated(width, height);

            ViewExtensions.CancelAnimations(this);

            if (wasshowing) TranslationX = 0.0;
            else TranslationX = width;
        }

        public void OnCellTapped(object sender, EventArgs e)
        {
            var mi = ((Cell)sender);
            var item = mi.BindingContext as Models.Ao3PageViewModel;
            if (item != null)
            {
                if (item.BaseData.Type == Models.Ao3PageType.Work && item.BaseData.Details != null && item.BaseData.Details.WorkId != 0)
                {
                    wpv.NavigateToLast(item.BaseData.Details.WorkId);
                }
                else
                {
                    wpv.Navigate(item.Uri);
                }
            }

        }

        private bool MenuOpenLastFilter(RLMenuItem mi)
        {
            var item = mi.CommandParameter as Models.Ao3PageViewModel;
            if (item?.BaseData == null) return true;

            return item.BaseData.Type == Models.Ao3PageType.Work || item.BaseData.Type == Models.Ao3PageType.Other;
        }

        private void OnCellAppearing(object sender, EventArgs e)
        {
            var cell = ((Cell)sender);
            for (int i = 0; i < cell.ContextActions.Count; i++)
            {
                var mi = cell.ContextActions[i] as RLMenuItem;
                if (mi != null && mi.OnFilter() == false)
                {
                    cell.ContextActions.RemoveAt(i);
                    i--;
                }
            }
        }

        public void OnMenuOpenLast(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var item = mi.CommandParameter as Models.Ao3PageViewModel;
            if (item != null)
            {
                if (item.BaseData.Type == Models.Ao3PageType.Work && item.BaseData.Details != null && item.BaseData.Details.WorkId != 0)
                {
                    wpv.NavigateToLast(item.BaseData.Details.WorkId);
                }
                else
                {
                    wpv.Navigate(item.Uri);
                }
            }
        }

        public void OnMenuOpen(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var item = mi.CommandParameter as Models.Ao3PageViewModel;
            if (item != null)
            {
                wpv.Navigate(item.Uri);
            }
        }

        public void OnMenuDelete(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var item = mi.CommandParameter as Models.Ao3PageViewModel;
            if (item != null)
            {
                readingListBacking.Remove(item);
                App.Database.DeleteReadingListItems(item.Uri.AbsoluteUri);
            }

        }

        public void OnAddPage(object sender, EventArgs e)
        {
            AddAsync(wpv.Current.AbsoluteUri);
        }

        public void OnRefresh(object sender, EventArgs e)
        {
            ListView.Focus();
            RefreshButton.IsEnabled = false;
            Task.Run(async () =>
            {
                List<Task> tasks = new List<Task>();
                foreach (var i in readingListBacking.All)
                {
                    tasks.Add(RefreshAsync(i));
                }

                await Task.WhenAll(tasks);

                wpv.DoOnMainThread(() =>
                {
                    RefreshButton.IsEnabled = true;
                });
            });
        }

        public void OnClose(object sender, EventArgs e)
        {
            IsOnScreen = false;
        }

        public Task AddAsync(string href)
        {
            if (readingListBacking.Find((m) => m.Uri.AbsoluteUri == href) != null)
                return null;

            return Task.Run(async () =>
            {
                var models = Data.Ao3SiteDataLookup.LookupQuick(new[] { href });
                var model = models.SingleOrDefault();
                if (model.Value == null) return;

                if (readingListBacking.Find((m) => m.Uri.AbsoluteUri == model.Value.Uri.AbsoluteUri) != null)
                    return;

                var viewmodel = new Models.Ao3PageViewModel { BaseData = model.Value };
                App.Database.SaveReadingListItems(new Models.ReadingList { Uri = model.Value.Uri.AbsoluteUri, PrimaryTag = model.Value.PrimaryTag, Title = model.Value.Title });
                wpv.DoOnMainThread(() =>
                {
                    readingListBacking.Add(viewmodel);
                });
                await RefreshAsync(viewmodel);
            });
        }

        public Task RefreshAsync(Models.Ao3PageViewModel viewmodel)
        {
            return Task.Run(async () =>
            {
                    var models = await Data.Ao3SiteDataLookup.LookupAsync(new[] { viewmodel.Uri.AbsoluteUri });

                    var model = models.SingleOrDefault();
                    if (model.Value != null)
                    {
                        if (viewmodel.Uri.AbsoluteUri != model.Value.Uri.AbsoluteUri)
                        {
                            App.Database.DeleteReadingListItems(viewmodel.Uri.AbsoluteUri);

                            var pvm = readingListBacking.Find((m) => m.Uri.AbsoluteUri == model.Value.Uri.AbsoluteUri);
                            if (pvm != null) readingListBacking.Remove(pvm);
                        }
                        App.Database.SaveReadingListItems(new Models.ReadingList { Uri = model.Value.Uri.AbsoluteUri, PrimaryTag = model.Value.PrimaryTag, Title = model.Value.Title });
                        updateBag.Add(new Tuple<Models.Ao3PageViewModel, Models.Ao3PageModel>(viewmodel, model.Value));
                        wpv.DoOnMainThread(() =>
                        {
                            while (!updateBag.IsEmpty)
                            {
                                Tuple<Models.Ao3PageViewModel, Models.Ao3PageModel> tuple;
                                if (updateBag.TryTake(out tuple))
                                    tuple.Item1.BaseData = tuple.Item2;
                            }
                        });
                    }
            });
        }
    }
}
