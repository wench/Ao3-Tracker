using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Color = Xamarin.Forms.Color;
using ImageSource = Xamarin.Forms.ImageSource;
using UriImageSource = Xamarin.Forms.UriImageSource;
using System.Linq;
using Ao3TrackReader.Resources;
using Ao3TrackReader.Data;
using System.Threading.Tasks;

namespace Ao3TrackReader.Models
{
    // How this stuff is intended to be formatted
    //
    // A work:
    // <ICON> <Title> by <User> (for <User>)
    // <ICON>  


    public class Ao3PageViewModel : IGroupable<Ao3PageViewModel>, INotifyPropertyChanged, INotifyPropertyChanging
    {
        public int CompareTo(Ao3PageViewModel y)
        {
            // Pages with no base data go last
            if (baseData == y.baseData) return 0;
            if (baseData == null) return 1;
            if (y.baseData == null) return -1;

            int c;

            // Sort based on chapters remaining
            if ((baseData.Type == Ao3PageType.Work || baseData.Type == Ao3PageType.Series) && (y.baseData.Type == Ao3PageType.Work || y.baseData.Type == Ao3PageType.Series))
            {
                int? xc = ChaptersRead;
                int? yc = y.ChaptersRead;
                if (xc != null || yc != null)
                {
                    if (xc == null) return 1;
                    if (yc == null) return -1;
                    xc = baseData.Details.Chapters.Available - xc;
                    yc = y.baseData.Details.Chapters.Available - yc;
                    c = xc.Value.CompareTo(yc.Value);
                    if (c != 0) return -c; // higher numbers of unread chapters first
                }
            }

            // Sort based on the page type
            c = baseData.Type.CompareTo(y.baseData.Type);
            if (c != 0) return c;

            // Sort based on title
            string tx = Title?.ToString();
            string ty = y.Title?.ToString();

            if (tx != ty)
            {
                if (tx == null) return 1;
                if (ty == null) return -1;
                c = StringComparer.OrdinalIgnoreCase.Compare(tx, ty);
                if (c != 0) return c;
            }

            string ux = Uri?.AbsoluteUri;
            string uy = y.Uri?.AbsoluteUri;
            if (ux == uy) return 0;
            if (ux == null) return 1;
            if (uy == null) return -1;
            return StringComparer.OrdinalIgnoreCase.Compare(ux, uy);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event System.ComponentModel.PropertyChangingEventHandler PropertyChanging;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanging(string propertyName)
        {
            PropertyChanging?.Invoke(this, new System.ComponentModel.PropertyChangingEventArgs(propertyName));
        }

        public Uri Uri { get; private set; }

        public string Group { get; private set; }
        public string GroupType { get; private set; }
        public TextTree Title { get; private set; }
        public string Date { get; private set; }
        public string Subtitle { get; private set; }
        public string Details { get; private set; }

        public int? ChaptersRead { get; private set; }
        public int? Unread { get; private set; }

        private Uri imageRatingUri;
        public ImageSource ImageRating {
            get {
                if (imageRatingUri == null) return null;
                return new UriImageSource { Uri = imageRatingUri, CachingEnabled = true, CacheValidity = TimeSpan.FromDays(14) };
            }
        }

        private Uri imageWarningsUri;
        public ImageSource ImageWarnings {
            get {
                if (imageWarningsUri == null) return null;
                return new UriImageSource { Uri = imageWarningsUri, CachingEnabled = true, CacheValidity = TimeSpan.FromDays(14) };
            }
        }

        private Uri imageCategoryUri;
        public ImageSource ImageCategory {
            get {
                if (imageCategoryUri == null) return null;
                return new UriImageSource { Uri = imageCategoryUri, CachingEnabled = true, CacheValidity = TimeSpan.FromDays(14) };
            }
        }

        private Uri imageCompleteUri;
        public ImageSource ImageComplete {
            get {
                if (imageCompleteUri == null) return null;
                return new UriImageSource { Uri = imageCompleteUri, CachingEnabled = true, CacheValidity = TimeSpan.FromDays(14) };
            }
        }


        public TextTree Summary { get; private set; }

        public bool SummaryVisible { get { return Summary != null; } }


        SortedDictionary<Ao3TagType, List<string>> tags;
        public TextTree Tags
        {
            get
            {
                var fs = new Span();

                if (tags != null)
                {
                    foreach (var t in tags)
                    {
                        if (t.Key == Ao3TagType.Fandoms) continue;

                        var s = new Span();

                        if (t.Key == Ao3TagType.Warnings)
                        {
                            s.Foreground = Colors.Base.High;
                            s.Bold = true;
                        }
                        else if (t.Key == Ao3TagType.Relationships)
                            s.Foreground = Colors.Base.MediumHigh;
                        else if (t.Key == Ao3TagType.Characters)
                        {
                            s.Foreground = Colors.Base.MediumLow;
                        }
                        else
                            s.Foreground = Colors.Base.Low;

                        foreach (var tag in t.Value)
                        {
                            s.Nodes.Add(new TextNode { Text = tag.Replace(' ', '\xA0'), Underline = true });
                            s.Nodes.Add(new TextNode { Text = ",  " });
                        }

                        if (s.Nodes.Count != 0)
                            fs.Nodes.Add(s);
                    }
                }
                if (fs.Nodes.Count != 0)
                {
                    var last = fs.Nodes[fs.Nodes.Count - 1] as Span;
                    last.Nodes.RemoveAt(last.Nodes.Count - 1);
                }
                return fs;
            }
        }

        public bool TagsVisible
        {
            get
            {

                if (tags != null)
                {
                    foreach (var t in tags)
                    {
                        if (t.Key == Ao3TagType.Fandoms) continue;
                        if (t.Value != null && t.Value.Count > 0)
                            return true;
                    }
                }
                return false;
            }
        }

        EventHandler<Work> ChapterNumberChangedDelegate;

        class Weak
        {
            WeakReference<Ao3PageViewModel> weakref;
            public Weak(Ao3PageViewModel target) { weakref = new WeakReference<Ao3PageViewModel>(target); }

            public void ChapterNumberChanged(object sender, Work workchap)
            {
                Ao3PageViewModel target;
                if (weakref.TryGetTarget(out target))
                    target.ChapterNumberChanged(sender, workchap);
            }
        }


        void Register()
        {
            if (baseData.Type == Ao3PageType.Work)
            {
                if (baseData.Details != null)
                {
                    var workevents = WorkEvents.GetEvent(baseData.Details.WorkId);
                    workevents.ChapterNumChanged += ChapterNumberChangedDelegate;
                }
            }
            else if (baseData.Type == Ao3PageType.Series)
            {
                if (baseData.SeriesWorks != null)
                {
                    foreach (var workdata in baseData.SeriesWorks)
                    {
                        var workevents = WorkEvents.GetEvent(workdata.Details.WorkId);
                        workevents.ChapterNumChanged += ChapterNumberChangedDelegate;
                    }
                }
            }
        }

        void Deregister()
        {
            if (baseData == null || baseData.Type != Ao3PageType.Work && baseData.Type != Ao3PageType.Series)
                return;

            if (ChapterNumberChangedDelegate == null)
            {
                Weak weak = new Weak(this);
                ChapterNumberChangedDelegate = new EventHandler<Work>(weak.ChapterNumberChanged);
            }

            if (baseData.Type == Ao3PageType.Work)
            {
                if (baseData.Details != null)
                {
                    var workevents = WorkEvents.TryGetEvent(baseData.Details.WorkId);
                    if (workevents != null) workevents.ChapterNumChanged += ChapterNumberChangedDelegate;
                }
            }
            else if (baseData.Type == Ao3PageType.Series)
            {
                if (baseData.SeriesWorks != null)
                {
                    foreach (var workdata in baseData.SeriesWorks)
                    {
                        var workevents = WorkEvents.TryGetEvent(workdata.Details.WorkId);
                        if (workevents != null) workevents.ChapterNumChanged += ChapterNumberChangedDelegate;
                    }
                }
            }
        }


        ~Ao3PageViewModel()
        {
            Deregister();
            ChapterNumberChangedDelegate = null;
        }

        void ChapterNumberChanged(object sender, Work workchap)
        {
            if (baseData.Type != Ao3PageType.Work && baseData.Type != Ao3PageType.Series)
                return;

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                RecalculateChaptersAsync().ContinueWith((task) =>
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                    {
                        OnPropertyChanging("Unread");
                        OnPropertyChanging("ChaptersRead");

                        int? newunread = ChaptersRead = task.Result;
                        if (newunread != null)
                        {
                            newunread = baseData.Details.Chapters.Available - newunread;

                        }
                        if (Unread != newunread)
                        {
                            Unread = newunread;
                        }

                        OnPropertyChanging("Title");
                        UpdateTitle();
                        OnPropertyChanged("Title");

                        OnPropertyChanging("Details");
                        UpdateDetails();
                        OnPropertyChanged("Details");

                        if (baseData.Type == Ao3PageType.Series)
                        {
                            OnPropertyChanging("Summary");
                            UpdateSummary();
                            OnPropertyChanged("Summary");
                        }

                        OnPropertyChanged("ChaptersRead");
                        OnPropertyChanged("Unread");

                    });
                });
            });
        }

        Ao3PageModel baseData;

        public Ao3PageModel BaseData
        {
            get { return baseData; }
            set
            {
                if (baseData == value) return;

                OnPropertyChanging("");
                Deregister();
                baseData = value;
                Unread = null;
                ChaptersRead = null;

                // Generate everything from Ao3PageModel 
                string newGroup = baseData.PrimaryTag ?? "";
                if (Group != newGroup)
                {
                    Group = newGroup;
                }

                if (!string.IsNullOrWhiteSpace(newGroup))
                {
                    string newgrouptype = baseData.PrimaryTagType.ToString().TrimEnd('s');
                    if (newgrouptype != GroupType)
                    {
                        GroupType = newgrouptype;
                    }
                }

                imageRatingUri = baseData.GetRequiredTagUri(Ao3RequiredTag.Rating);
                imageWarningsUri = baseData.GetRequiredTagUri(Ao3RequiredTag.Warnings);
                imageCategoryUri = baseData.GetRequiredTagUri(Ao3RequiredTag.Category);
                imageCompleteUri = baseData.GetRequiredTagUri(Ao3RequiredTag.Complete);

                Uri = baseData.Uri;

                Date = baseData.Details?.LastUpdated ?? "";

                Subtitle = "";
                if (baseData.Tags != null && baseData.Tags.ContainsKey(Ao3TagType.Fandoms)) Subtitle = string.Join(",  ", baseData.Tags[Ao3TagType.Fandoms].Select((s) => s.Replace(' ', '\xA0')));

                tags = baseData.Tags;

                UpdateTitle();
                UpdateSummary();
                UpdateDetails();

                OnPropertyChanged("");

                RecalculateChaptersAsync().ContinueWith((task) =>
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                    {
                        if (baseData != value)
                            return;

                        Register();

                        OnPropertyChanging("Unread");
                        OnPropertyChanging("ChaptersRead");

                        int? newunread = ChaptersRead = task.Result;
                        if (newunread != null)
                        {
                            newunread = baseData.Details.Chapters.Available - newunread;

                        }
                        if (Unread != newunread)
                        {
                            Unread = newunread;
                        }

                        OnPropertyChanging("Title");
                        UpdateTitle();
                        OnPropertyChanged("Title");

                        OnPropertyChanging("Details");
                        UpdateDetails();
                        OnPropertyChanged("Details");

                        if (baseData.Type == Ao3PageType.Series)
                        {
                            OnPropertyChanging("Summary");
                            UpdateSummary();
                            OnPropertyChanged("Summary");
                        }

                        OnPropertyChanged("ChaptersRead");
                        OnPropertyChanged("Unread");
                    });
                });
            }
        }

        public IDictionary<long, Helper.WorkChapter> WorkChapters { get; private set; }
        async Task<int?> RecalculateChaptersAsync()
        {
            if (baseData?.Details?.Chapters == null)
            {
                return null;
            }

            int? newread = null;
            try
            {
                if (baseData.Type == Ao3PageType.Work)
                {
                    WorkChapters = await App.Storage.getWorkChaptersAsync(new[] { baseData.Details.WorkId });
                    var workchap = WorkChapters.FirstOrDefault().Value;
                    long chapters_finished = workchap?.number ?? 0;
                    if (workchap?.location != null) { chapters_finished--; }
                    newread = (int)chapters_finished;
                }
                else if (baseData.Type == Ao3PageType.Series)
                {
                    WorkChapters = await App.Storage.getWorkChaptersAsync(baseData.SeriesWorks.Select(pm => pm.Details.WorkId));
                    long chapters_finished = 0;
                    foreach (var workchap in WorkChapters.Values)
                    {
                        chapters_finished += workchap.number;
                        if (workchap.location != null) { chapters_finished--; }

                    }
                    newread = (int)chapters_finished;
                }
                else
                {
                    WorkChapters = null;
                    newread = null;
                }
            }
            catch (Exception)
            {
                WorkChapters = null;
                newread = null;
            }

            return newread;
        }

        void UpdateTitle()
        {
            var ts = new Span();

            if (baseData.Type == Ao3PageType.Search || baseData.Type == Ao3PageType.Bookmarks || baseData.Type == Ao3PageType.Tag)
            {
                if (!string.IsNullOrWhiteSpace(baseData.PrimaryTag))
                {
                    ts.Nodes.Add(baseData.PrimaryTag);
                    if (!string.IsNullOrWhiteSpace(baseData.Title)) ts.Nodes.Add(new TextNode { Text = " - ", Foreground = Colors.Base });
                }
            }

            if (baseData.Type == Ao3PageType.Series) ts.Nodes.Add(new TextNode { Text = "Series ", Foreground = Colors.Base });

            if (!string.IsNullOrWhiteSpace(baseData.Title)) ts.Nodes.Add(baseData.Title);
            if (baseData.Details?.Authors != null && baseData.Details.Authors.Count != 0)
            {
                ts.Nodes.Add(new TextNode { Text = " by ", Foreground = Colors.Base });
                bool first = true;
                foreach (var user in baseData.Details.Authors)
                {
                    if (!first)
                        ts.Nodes.Add(new TextNode { Text = ", ", Foreground = Colors.Base });
                    else
                        first = false;

                    ts.Nodes.Add(user.Value);
                }
            }
            if (baseData.Details?.Recipiants != null && baseData.Details.Recipiants.Count != 0)
            {
                ts.Nodes.Add(new TextNode { Text = " for ", Foreground = Colors.Base });
                bool first = true;
                foreach (var user in baseData.Details.Recipiants)
                {
                    if (!first)
                        ts.Nodes.Add(new TextNode { Text = ", ", Foreground = Colors.Base });
                    else
                        first = false;

                    ts.Nodes.Add(user.Value);
                }
            }

            if (Unread != null && Unread > 0)
            {
                ts.Nodes.Add(new TextNode { Text = "  " + Unread.ToString() + " unread chapter" + (Unread == 1 ? "" : "s"), Foreground = Colors.Base });
            }

            var oldtitle = Title;
            if (ts.Nodes.Count == 0) Title = Uri.PathAndQuery;
            else Title = ts;

        }

        void UpdateDetails()
        {
            List<string> d = new List<string>();
            if (!string.IsNullOrWhiteSpace(baseData.Language)) d.Add("Language: " + baseData.Language);
            if (baseData.Details != null)
            {
                if (baseData.Details.Words != null) d.Add("Words:\xA0" + baseData.Details.Words.ToString());

                if (baseData.Details.Chapters != null)
                {
                    string readstr = "";
                    if (ChaptersRead != null) readstr = ChaptersRead.ToString() + "/";
                    d.Add("Chapters:\xA0" + readstr + baseData.Details.Chapters.Available.ToString() + "/" + (baseData.Details.Chapters.Total?.ToString() ?? "?"));
                }

                if (baseData.Details.Collections != null) d.Add("Collections:\xA0" + baseData.Details.Collections.ToString());
                if (baseData.Details.Comments != null) d.Add("Comments:\xA0" + baseData.Details.Comments.ToString());
                if (baseData.Details.Kudos != null) d.Add("Kudos:\xA0" + baseData.Details.Kudos.ToString());
                if (baseData.Details.Bookmarks != null) d.Add("Bookmarks:\xA0" + baseData.Details.Bookmarks.ToString());
                if (baseData.Details.Hits != null) d.Add("Hits:\xA0" + baseData.Details.Hits.ToString());
            }

            Details = string.Join("   ", d);
            if (!string.IsNullOrWhiteSpace(baseData.SearchQuery)) Details = ("Query: " + baseData.SearchQuery + "\n" + Details).Trim();
            if (string.IsNullOrWhiteSpace(Details)) Details = ("Uri: " + Uri.AbsoluteUri).Trim();
        }

        void UpdateSummary()
        {
            if (baseData.Details?.Summary != null)
            {
                Summary = baseData.Details?.Summary;
            }
            else if (baseData.Type == Ao3PageType.Series)
            {
                var summary = new Block();
                if (baseData.SeriesWorks != null) foreach (var workmodel in baseData.SeriesWorks)
                {
                    Helper.WorkChapter workchap;
                    int? unread = null;
                    if (WorkChapters != null)
                    {
                        int chapters_finished = 0;
                        if (WorkChapters.TryGetValue(workmodel.Details.WorkId, out workchap))
                        {
                            chapters_finished = (int)workchap.number;
                            if (workchap.location != null) { chapters_finished--; }
                        }
                        unread = workmodel.Details.Chapters.Available - chapters_finished;
                     }
                    var worksummary = new Span();

                    var ts = new Span { Foreground = Resources.Colors.Highlight.Low };

                    if (!string.IsNullOrWhiteSpace(workmodel.Title)) ts.Nodes.Add(new TextNode { Text = workmodel.Title, Foreground = Resources.Colors.Highlight.MediumHigh });
                    if (workmodel.Details?.Authors != null && workmodel.Details.Authors.Count != 0)
                    {
                        ts.Nodes.Add(new TextNode { Text = " by ", Foreground = Resources.Colors.Base.MediumLow });
                        bool first = true;
                        foreach (var user in workmodel.Details.Authors)
                        {
                            if (!first)
                                ts.Nodes.Add(new TextNode { Text = ", ", Foreground = Resources.Colors.Base.MediumLow });
                            else
                                first = false;

                            ts.Nodes.Add(user.Value);
                        }
                    }
                    if (workmodel.Details?.Recipiants != null && workmodel.Details.Recipiants.Count != 0)
                    {
                        ts.Nodes.Add(new TextNode { Text = " for ", Foreground = Resources.Colors.Base.MediumLow });
                        bool first = true;
                        foreach (var user in workmodel.Details.Recipiants)
                        {
                            if (!first)
                                ts.Nodes.Add(new TextNode { Text = ", ", Foreground = Resources.Colors.Base.MediumLow });
                            else
                                first = false;

                            ts.Nodes.Add(user.Value);
                        }
                    }
                    if (unread != null && unread > 0)
                    {
                        ts.Nodes.Add(new TextNode { Text = "  " + unread.ToString() + " unread chapter" + (unread == 1 ? "" : "s"), Foreground = Resources.Colors.Base.MediumHigh });
                    }

                    worksummary.Nodes.Add(ts);

                    summary.Nodes.Add(worksummary);
                    summary.Nodes.Add("\n");
                }
                Summary = summary;
            }
            else
            {
                Summary = null;
            }
        }


    }
}
