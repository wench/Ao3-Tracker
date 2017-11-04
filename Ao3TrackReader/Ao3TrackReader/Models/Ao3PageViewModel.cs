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
using System.ComponentModel;
using System.Text;
using Color = Xamarin.Forms.Color;
using ImageSource = Xamarin.Forms.ImageSource;
using UriImageSource = Xamarin.Forms.UriImageSource;
using System.Linq;
using Ao3TrackReader.Resources;
using Ao3TrackReader.Data;
using System.Threading.Tasks;

#if WINDOWS_APP || WINDOWS_PHONE_APP
using PropertyChangingEventHandler = Xamarin.Forms.PropertyChangingEventHandler;
using PropertyChangingEventArgs = Xamarin.Forms.PropertyChangingEventArgs;
#else
using PropertyChangingEventHandler = System.ComponentModel.PropertyChangingEventHandler;
using PropertyChangingEventArgs = System.ComponentModel.PropertyChangingEventArgs;
#endif

namespace Ao3TrackReader.Models
{
    // How this stuff is intended to be formatted
    //
    // A work:
    // <ICON> <Title> by <User> (for <User>)
    // <ICON>  


    public sealed class Ao3PageViewModel : IGroupable, INotifyPropertyChanged, INotifyPropertyChanging, IDisposable, IComparable<Ao3PageViewModel>
    {
        public Ao3PageViewModel(Uri uri, int? unread, IDictionary<Ao3TagType,bool> tagvis)
        {
            Uri = uri;
            Unread = unread;

            if (tagvis == null)
                tagTypeVisible = new Dictionary<Ao3TagType, bool>();
            else
                tagTypeVisible = new Dictionary<Ao3TagType, bool>(tagvis);
        }

        public int CompareTo(Ao3PageViewModel y)
        {
            // Pages with no base data go last
            if (baseData == y.baseData) return 0;
            if (baseData == null) return 1;
            if (y.baseData == null) return -1;

            int c;

            // Sort based on chapters remaining
            if ((baseData.Type == Ao3PageType.Work || baseData.Type == Ao3PageType.Series || baseData.Type == Ao3PageType.Collection) && (y.baseData.Type == Ao3PageType.Work || y.baseData.Type == Ao3PageType.Series || y.baseData.Type == Ao3PageType.Collection))
            {
                int? xc = Unread;
                int? yc = y.Unread;
                if (xc != null || yc != null)
                {
                    if (xc == null) return 1;
                    if (yc == null) return -1;
                    c = xc.Value.CompareTo(yc.Value);
                    if (c != 0) return -c; // higher numbers of unread chapters first

                    // Completed works when we've read all the chapters get put bottom
                    if (xc == 0 && baseData?.Details?.Chapters != null && y.baseData?.Details?.Chapters != null)
                    {
                        bool xf = baseData.Details.Chapters.Available == baseData.Details.Chapters.Total;
                        bool yf = y.baseData.Details.Chapters.Available == y.baseData.Details.Chapters.Total;
                        c = xf.CompareTo(yf);
                        if (c != 0) return c;
                    }
                }
            }
            else
            {
                // Sort based on the page type
                c = baseData.Type.CompareTo(y.baseData.Type);
                if (c != 0) return c;
            }

            // Sort based on title
            string tx = baseData.Title;
            string ty = y.baseData.Title;

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
        public event PropertyChangingEventHandler PropertyChanging;

        void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        void OnPropertyChanging(string propertyName)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        bool isSelected = false;
        public bool IsSelected {
            get { return isSelected; }
            set
            {
                if (value == isSelected) return;
                OnPropertyChanging("IsSelected");
                OnPropertyChanging("ShouldHide");
                isSelected = value;
                OnPropertyChanged("ShouldHide");
                OnPropertyChanged("IsSelected");
            }
        }


        public Uri Uri { get; private set; }

        public string Group { get; private set; }
        public string GroupType { get; private set; }
        public Text.TextEx Title { get; private set; }
        public DateTime? Date { get; private set; }
        public Text.TextEx Subtitle { get; private set; }
        public Text.TextEx Details { get; private set; }

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


        public Text.TextEx Summary { get; private set; }

        public bool SummaryVisible { get { return Summary != null && !Summary.IsEmpty; } }

        Dictionary<Ao3TagType, bool> tagTypeVisible;
        public void SetTagVisibilities(Ao3TagType type, bool visible)
        {
            if (!TagsVisible || type == Ao3TagType.Fandoms || !tags.TryGetValue(type,out var t) || t.Count == 0)
                return;

            if (!tagTypeVisible.TryGetValue(type, out var existing))
                existing = true;

            if (existing == visible)
                return;

            OnPropertyChanging("Tags");
            tagTypeVisible[type] = visible;
            OnPropertyChanged("Tags");
        }

        SortedDictionary<Ao3TagType, List<string>> tags;
        public Text.TextEx Tags
        {
            get
            {
                var fs = new Text.Span();

                if (tags != null)
                {
                    foreach (var t in tags)
                    {
                        if (t.Key == Ao3TagType.Fandoms || (tagTypeVisible.TryGetValue(t.Key,out var vis) && !vis))
                            continue;

                        var s = new Text.Span();

                        if (t.Key == Ao3TagType.Warnings)
                        {
                            s.Foreground = Colors.Base.High;
                            s.Bold = true;
                        }
                        else if (t.Key == Ao3TagType.Rating || t.Key == Ao3TagType.Category || t.Key == Ao3TagType.Complete)
                            s.Foreground = Colors.Base.High;
                        else if (t.Key == Ao3TagType.Relationships)
                            s.Foreground = Colors.Base.MediumHigh;
                        else if (t.Key == Ao3TagType.Characters)
                        {
                            s.Foreground = Colors.Base.Medium;
                        }
                        else
                            s.Foreground = Colors.Base.MediumLow;

                        foreach (var tag in t.Value)
                        {
                            s.Nodes.Add(new Text.String { Text = tag.Replace(' ', '\xA0'), Underline = true });
                            s.Nodes.Add(new Text.String { Text = ",  " });
                        }

                        if (s.Nodes.Count != 0)
                            fs.Nodes.Add(s);
                    }
                }
                if (fs.Nodes.Count != 0)
                {
                    var last = fs.Nodes[fs.Nodes.Count - 1] as Text.Span;
                    last.Nodes.RemoveAt(last.Nodes.Count - 1);
                }
                return fs;
            }
        }

        public bool ShouldHide { get { return Unread == 0 && BaseData?.Details?.IsComplete != true && !isSelected; } }


        bool tags_visible = true;
        public bool TagsVisible
        {
            get
            {
                if (BaseData.Type == Ao3PageType.Work && tags != null && !SummaryVisible)
                    return true;

                if (tags_visible && tags != null)
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
            set
            {
                if (tags_visible != value)
                {
                    OnPropertyChanging("TagsVisible");
                    tags_visible = value;
                    OnPropertyChanged("TagsVisible");
                }
            }
        }

        void Register()
        {
            if (baseData.Type == Ao3PageType.Work)
            {
                if (baseData.Details != null)
                {
                    var workevents = WorkEvents.GetEvent(baseData.Details.WorkId);
                    workevents.ChapterNumChanged += ChapterNumberChanged;
                }
            }
            else if (baseData.Type == Ao3PageType.Series || baseData.Type == Ao3PageType.Collection)
            {
                if (baseData.SeriesWorks != null)
                {
                    foreach (var workdata in baseData.SeriesWorks)
                    {
                        var workevents = WorkEvents.GetEvent(workdata.Details.WorkId);
                        workevents.ChapterNumChanged += ChapterNumberChanged;
                    }
                }
            }
        }

        void Deregister()
        {
            if (baseData == null || (baseData.Type != Ao3PageType.Work && baseData.Type != Ao3PageType.Series && baseData.Type != Ao3PageType.Collection))
                return;

            if (baseData.Type == Ao3PageType.Work)
            {
                if (baseData.Details != null)
                {
                    var workevents = WorkEvents.TryGetEvent(baseData.Details.WorkId);
                    if (workevents != null) workevents.ChapterNumChanged -= ChapterNumberChanged;
                }
            }
            else if (baseData.Type == Ao3PageType.Series || baseData.Type == Ao3PageType.Collection)
            {
                if (baseData.SeriesWorks != null)
                {
                    foreach (var workdata in baseData.SeriesWorks)
                    {
                        var workevents = WorkEvents.TryGetEvent(workdata.Details.WorkId);
                        if (workevents != null) workevents.ChapterNumChanged -= ChapterNumberChanged;
                    }
                }
            }
        }


        void ChapterNumberChanged(object sender, EventArgs<Work> args)
        {
            var workchap = args.Value;

            if (baseData.Type != Ao3PageType.Work && baseData.Type != Ao3PageType.Series && baseData.Type != Ao3PageType.Collection)
                return;

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                RecalculateChaptersAsync().ContinueWith((task) =>
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                    {
                        OnPropertyChanging("SortOrder");
                        OnPropertyChanging("Unread");
                        OnPropertyChanging("ChaptersRead");

                        ChaptersRead = task.Result;
                        if (ChaptersRead != null && baseData?.Details?.Chapters?.Available != null)
                        {
                            Unread = baseData.Details.Chapters.Available - ChaptersRead;
                        }

                        OnPropertyChanging("Title");
                        UpdateTitle();
                        OnPropertyChanged("Title");

                        OnPropertyChanging("Details");
                        UpdateDetails();
                        OnPropertyChanged("Details");

                        if (baseData.Type == Ao3PageType.Series || baseData.Type == Ao3PageType.Collection)
                        {
                            OnPropertyChanging("Summary");
                            UpdateSummary();
                            OnPropertyChanged("Summary");
                        }

                        OnPropertyChanged("ChaptersRead");
                        OnPropertyChanged("Unread");
                        OnPropertyChanged("SortOrder");

                    });
                });
            });
        }

        public bool HasChapters => baseData.Type == Ao3PageType.Work || baseData.Type == Ao3PageType.Series || baseData.Type == Ao3PageType.Collection;

        Ao3PageModel baseData;

        public Ao3PageModel BaseData
        {
            get { return baseData; }
        }

        public async Task SetBaseDataAsync(Ao3PageModel value)
        {
            if (value == null) throw new ArgumentNullException("value", "BaseData can not be set to null.");

            if (value == BaseData) return;

            OnPropertyChanging("");
            Deregister();
            baseData = value;
            //Unread = null;
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

            Date = baseData.Details?.LastUpdated;// baseData.Details?.LastUpdated?.ToString("d MMM yyyy") ?? "";

            if (baseData.Tags != null && baseData.Tags.ContainsKey(Ao3TagType.Fandoms)) Subtitle = string.Join(",  ", baseData.Tags[Ao3TagType.Fandoms].Select((s) => s.Replace(' ', '\xA0')));
            else Subtitle = null;

            tags = baseData.Tags;

            UpdateTitle();
            UpdateSummary();
            UpdateDetails();

            OnPropertyChanged("");

            var chapsread = await RecalculateChaptersAsync();

            if (baseData != value) return;

            await WebViewPage.Current.DoOnMainThreadAsync(() =>
            {
                if (baseData != value) return;

                Register();

                OnPropertyChanging("SortOrder");
                OnPropertyChanging("Unread");
                OnPropertyChanging("ChaptersRead");

                ChaptersRead = chapsread;
                if (ChaptersRead != null && baseData?.Details?.Chapters?.Available != null)
                {
                    Unread = baseData.Details.Chapters.Available - ChaptersRead;
                }
                else if (!HasChapters)
                {
                    Unread = null;
                }

                OnPropertyChanging("Title");
                UpdateTitle();
                OnPropertyChanged("Title");

                OnPropertyChanging("Details");
                UpdateDetails();
                OnPropertyChanged("Details");

                if (baseData.Type == Ao3PageType.Series || baseData.Type == Ao3PageType.Collection)
                {
                    OnPropertyChanging("Summary");
                    UpdateSummary();
                    OnPropertyChanged("Summary");
                }

                OnPropertyChanged("ChaptersRead");
                OnPropertyChanged("Unread");
                OnPropertyChanged("SortOrder");
            });
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
                    WorkChapters = await App.Storage.GetWorkChaptersAsync(new[] { baseData.Details.WorkId });
                    var workchap = WorkChapters.FirstOrDefault().Value;
                    long chapters_finished = workchap?.number ?? 0;
                    if (workchap?.location != null) { chapters_finished--; }
                    newread = (int)chapters_finished;
                }
                else if (baseData.Type == Ao3PageType.Series || baseData.Type == Ao3PageType.Collection)
                {
                    WorkChapters = await App.Storage.GetWorkChaptersAsync(baseData.SeriesWorks.Select(pm => pm.Details.WorkId));
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
            var ts = new Text.Span();

            if (baseData.Type == Ao3PageType.Search || baseData.Type == Ao3PageType.Bookmarks || baseData.Type == Ao3PageType.Tag)
            {
                if (!string.IsNullOrWhiteSpace(baseData.PrimaryTag))
                {
                    ts.Nodes.Add(baseData.PrimaryTag);
                    if (!string.IsNullOrWhiteSpace(baseData.Title)) ts.Nodes.Add(new Text.String { Text = " - ", Foreground = Colors.Base });
                }
            }

            if (baseData.Type == Ao3PageType.Series) ts.Nodes.Add(new Text.String { Text = "Series ", Foreground = Colors.Base });
            else if (baseData.Type == Ao3PageType.Collection) ts.Nodes.Add(new Text.String { Text = "Collection ", Foreground = Colors.Base });

            if (!string.IsNullOrWhiteSpace(baseData.Title)) ts.Nodes.Add(baseData.Title);

            if (baseData.Type == Ao3PageType.Collection && Unread != null && Unread > 0)
            {
                ts.Nodes.Add(new Text.String { Text = "  " + Unread.ToString() + "\xA0unread chapter" + (Unread == 1 ? "" : "s"), Foreground = Colors.Base });
            }

            if (baseData.Details?.Authors != null && baseData.Details.Authors.Count != 0)
            {
                if (baseData.Type == Ao3PageType.Collection)
                    ts.Nodes.Add(new Text.String { Text = "\nMaintainers: ", Foreground = Colors.Base });
                else
                    ts.Nodes.Add(new Text.String { Text = " by ", Foreground = Colors.Base });

                bool first = true;
                foreach (var user in baseData.Details.Authors)
                {
                    if (!first)
                        ts.Nodes.Add(new Text.String { Text = ", ", Foreground = Colors.Base });
                    else
                        first = false;

                    ts.Nodes.Add(user.Value.Replace(' ', '\xA0'));
                }
            }
            if (baseData.Details?.Recipiants != null && baseData.Details.Recipiants.Count != 0)
            {
                ts.Nodes.Add(new Text.String { Text = " for ", Foreground = Colors.Base });
                bool first = true;
                foreach (var user in baseData.Details.Recipiants)
                {
                    if (!first)
                        ts.Nodes.Add(new Text.String { Text = ", ", Foreground = Colors.Base });
                    else
                        first = false;

                    ts.Nodes.Add(user.Value.Replace(' ', '\xA0'));
                }
            }

            if (baseData.Type != Ao3PageType.Collection && Unread != null && Unread > 0)
            {
                ts.Nodes.Add(new Text.String { Text = "  " + Unread.ToString() + "\xA0unread chapter" + (Unread == 1 ? "" : "s"), Foreground = Colors.Base });
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

            if (!string.IsNullOrWhiteSpace(baseData.SortColumn))
            {
                if (!string.IsNullOrWhiteSpace(baseData.SortDirection))
                {
                    d.Add("Order: " + baseData.SortColumn + " " + baseData.SortDirection);
                }
                else
                {
                    d.Add("Order: " + baseData.SortColumn);
                }
            }
            else if (!string.IsNullOrWhiteSpace(baseData.SortDirection))
            {
                d.Add("Order: " + baseData.SortDirection);
            }

            string ds = string.Join("   ", d);
            if (!string.IsNullOrWhiteSpace(baseData.SearchQuery)) ds = ("Query: " + baseData.SearchQuery + "\n" + ds).Trim();
            if (string.IsNullOrWhiteSpace(ds)) ds = ("Uri: " + Uri.AbsoluteUri).Trim();
            Details = ds;
        }

        void UpdateSummary()
        {
            if (baseData.Type == Ao3PageType.Series || baseData.Type == Ao3PageType.Collection)
            {
                var summary = new Text.Block();
                if (baseData.Details?.Summary != null) {
                    summary.Nodes.Add(baseData.Details.Summary);
                }
                if (baseData.SeriesWorks != null) foreach (var workmodel in baseData.SeriesWorks)
                {
                    int? unread = null;
                    if (WorkChapters != null)
                    {
                        int chapters_finished = 0;
                        if (WorkChapters.TryGetValue(workmodel.Details.WorkId, out Helper.WorkChapter workchap))
                        {
                            chapters_finished = (int)workchap.number;
                            if (workchap.location != null) { chapters_finished--; }
                        }
                        unread = workmodel.Details.Chapters.Available - chapters_finished;
                     }
                    var worksummary = new Text.Span();

                    var ts = new Text.Span { Foreground = Resources.Colors.Highlight.Medium };

                    if (!string.IsNullOrWhiteSpace(workmodel.Title)) ts.Nodes.Add(new Text.String { Text = workmodel.Title, Foreground = Resources.Colors.Highlight.MediumHigh });
                    if (workmodel.Details?.Authors != null && workmodel.Details.Authors.Count != 0)
                    {
                        ts.Nodes.Add(new Text.String { Text = " by ", Foreground = Resources.Colors.Base.Medium });
                        bool first = true;
                        foreach (var user in workmodel.Details.Authors)
                        {
                            if (!first)
                                ts.Nodes.Add(new Text.String { Text = ", ", Foreground = Resources.Colors.Base.Medium });
                            else
                                first = false;

                            ts.Nodes.Add(user.Value);
                        }
                    }
                    if (workmodel.Details?.Recipiants != null && workmodel.Details.Recipiants.Count != 0)
                    {
                        ts.Nodes.Add(new Text.String { Text = " for ", Foreground = Resources.Colors.Base.Medium });
                        bool first = true;
                        foreach (var user in workmodel.Details.Recipiants)
                        {
                            if (!first)
                                ts.Nodes.Add(new Text.String { Text = ", ", Foreground = Resources.Colors.Base.Medium });
                            else
                                first = false;

                            ts.Nodes.Add(user.Value);
                        }
                    }
                    if (unread != null && unread > 0)
                    {
                        ts.Nodes.Add(new Text.String { Text = "  " + unread.ToString() + "\xA0unread chapter" + (unread == 1 ? "" : "s"), Foreground = Resources.Colors.Base.MediumHigh });
                    }

                    worksummary.Nodes.Add(ts);

                    summary.Nodes.Add(worksummary);
                    summary.Nodes.Add("\n");
                }
                Summary = summary;
            }
            else
            {
                Summary = baseData.Details?.Summary;
            }
        }

        public bool HasUri(Uri check)
        {
            if (Uri == check) return true;

            if (baseData?.SeriesWorks != null)
            {
                foreach(var workmodel in baseData.SeriesWorks)
                {
                    if (workmodel.Uri == check)
                        return true;
                }
            }

            return false;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    Deregister();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Ao3PageViewModel() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
