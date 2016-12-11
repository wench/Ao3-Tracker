using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Color = Xamarin.Forms.Color;
using ImageSource = Xamarin.Forms.ImageSource;
using UriImageSource = Xamarin.Forms.UriImageSource;
using System.Linq;
using Ao3TrackReader.Controls;

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

            // Sort based on the page type
            int c = baseData.Type.CompareTo(y.baseData.Type);
            if (c != 0) return c;

            // Sort based on chapters remaining
            if (y.baseData.Type == Ao3PageType.Work)
            {
                int? xc = baseData?.Details?.Chapters?.Item1;
                int? yc = y.baseData?.Details?.Chapters?.Item1;
                if (xc != null || yc != null)
                {
                    if (xc == null) return 1;
                    if (yc == null) return -1;
                    xc = baseData.Details.Chapters.Item2 - xc;
                    yc = y.baseData.Details.Chapters.Item2 - yc;
                    c = xc.Value.CompareTo(yc.Value);
                    if (c != 0) return -c; // higher numbers of unread chapters first
                }
            }

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

        public int? Unread { get; private set; }

        private Uri imageRatingUri;
        public ImageSource ImageRating { get { return new UriImageSource { Uri = imageRatingUri }; } }

        private Uri imageWarningsUri;
        public ImageSource ImageWarnings { get { return new UriImageSource { Uri = imageWarningsUri }; } }

        private Uri imageCategoryUri;
        public ImageSource ImageCategory { get { return new UriImageSource { Uri = imageCategoryUri }; } }

        private Uri imageCompleteUri;
        public ImageSource ImageComplete { get { return new UriImageSource { Uri = imageCompleteUri }; } }


        public TextTree Summary { get; private set; }

        public bool SummaryVisible { get { return Summary != null ; } }

        
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
                            s.Foreground = App.Colors["SystemBaseHighColor"];
                            s.Bold = true;
                        }
                        else if (t.Key == Ao3TagType.Relationships)
                            s.Foreground = App.Colors["SystemChromeAltLowColor"];
                        else if (t.Key == Ao3TagType.Characters)
                        {
                            var c1 = App.Colors["SystemChromeAltLowColor"];
                            var c2 = App.Colors["SystemChromeHighColor"];
                            s.Foreground = new Color((c1.R + c2.R) / 2, (c1.G + c2.G) / 2, (c1.B + c2.B) / 2);
                        }
                        else
                            s.Foreground = App.Colors["SystemChromeHighColor"];

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

        Ao3PageModel baseData;
        public Ao3PageModel BaseData
        {
            get { return baseData; }
            set
            {
                if (baseData == value) return;

                OnPropertyChanging("");
                try
                {
                    baseData = value;

                    int? newunread = value.Details?.Chapters?.Item1;
                    if (newunread != null)
                    {
                        newunread = value.Details.Chapters.Item2 - newunread;

                    }
                    if (Unread != newunread)
                    {
                        Unread = newunread;
                    }

                    // Generate everything from Ao3PageModel 
                    string newGroup = value.PrimaryTag ?? "";
                    if (Group != newGroup)
                    {
                        Group = newGroup;
                    }

                    if (!string.IsNullOrWhiteSpace(newGroup))
                    {
                        string newgrouptype = value.PrimaryTagType.ToString().TrimEnd('s');
                        if (newgrouptype != GroupType)
                        {
                            GroupType = newgrouptype;
                        }
                    }

                    imageRatingUri = value.GetRequiredTagUri(Ao3RequiredTag.Rating);
                    imageWarningsUri = value.GetRequiredTagUri(Ao3RequiredTag.Warnings);
                    imageCategoryUri = value.GetRequiredTagUri(Ao3RequiredTag.Category);
                    imageCompleteUri = value.GetRequiredTagUri(Ao3RequiredTag.Complete);

                    Uri = value.Uri;


                    var ts = new Span();

                    if (value.Type == Ao3PageType.Search || value.Type == Ao3PageType.Bookmarks || value.Type == Ao3PageType.Tag)
                    {
                        if (!string.IsNullOrWhiteSpace(value.PrimaryTag))
                        {
                            ts.Nodes.Add(value.PrimaryTag);
                            if (!string.IsNullOrWhiteSpace(value.Title)) ts.Nodes.Add(new TextNode { Text = " - ", Foreground = App.Colors["SystemBaseHighColor"] });
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(value.Title)) ts.Nodes.Add(value.Title);
                    if (value.Details?.Authors != null && value.Details.Authors.Count != 0)
                    {
                        ts.Nodes.Add(new TextNode { Text = " by ", Foreground = App.Colors["SystemBaseHighColor"] });
                        bool first = true;
                        foreach (var user in value.Details.Authors)
                        {
                            if (!first)
                                ts.Nodes.Add(new TextNode { Text = ", ", Foreground = App.Colors["SystemBaseHighColor"] });
                            else
                                first = false;

                            ts.Nodes.Add(user.Value);
                        }
                    }
                    if (value.Details?.Recipiants != null && value.Details.Recipiants.Count != 0)
                    {
                        ts.Nodes.Add(new TextNode { Text = " for ", Foreground = App.Colors["SystemBaseHighColor"] });
                        bool first = true;
                        foreach (var user in value.Details.Recipiants)
                        {
                            if (!first)
                                ts.Nodes.Add(new TextNode { Text = ", ", Foreground = App.Colors["SystemBaseHighColor"] });
                            else
                                first = false;

                            ts.Nodes.Add(user.Value);
                        }
                    }

                    if (Unread != null && Unread > 0)
                    {
                        ts.Nodes.Add(new TextNode { Text = "  " + Unread.ToString() + " unread chapter" + (Unread == 1 ? "" : "s"), Foreground = App.Colors["SystemBaseHighColor"] });
                    }

                    var oldtitle = Title;
                    if (ts.Nodes.Count == 0) Title = Uri.PathAndQuery;
                    else Title = ts;

                    Date = value.Details?.LastUpdated ?? "";

                    Subtitle = "";
                    if (value.Tags != null && value.Tags.ContainsKey(Ao3TagType.Fandoms)) Subtitle = string.Join(",  ", value.Tags[Ao3TagType.Fandoms].Select((s) => s.Replace(' ', '\xA0')));

                    List<string> d = new List<string>();
                    if (!string.IsNullOrWhiteSpace(value.Language)) d.Add("Language: " + value.Language);
                    if (value.Details != null)
                    {
                        if (value.Details.Words != null) d.Add("Words:\xA0" + value.Details.Words.ToString());

                        if (value.Details.Chapters != null)
                        {
                            string readstr = "";
                            if (value.Details.Chapters.Item1 != null) readstr = value.Details.Chapters.Item1.ToString() + "/";
                            d.Add("Chapters:\xA0" + readstr + value.Details.Chapters.Item2.ToString() + "/" + (value.Details.Chapters.Item3?.ToString() ?? "?"));
                        }

                        if (value.Details.Collections != null) d.Add("Collections:\xA0" + value.Details.Collections.ToString());
                        if (value.Details.Comments != null) d.Add("Comments:\xA0" + value.Details.Comments.ToString());
                        if (value.Details.Kudos != null) d.Add("Kudos:\xA0" + value.Details.Kudos.ToString());
                        if (value.Details.Bookmarks != null) d.Add("Bookmarks:\xA0" + value.Details.Bookmarks.ToString());
                        if (value.Details.Hits != null) d.Add("Hits:\xA0" + value.Details.Hits.ToString());
                    }

                    Details = string.Join("   ", d);
                    if (!string.IsNullOrWhiteSpace(value.SearchQuery)) Details = ("Query: " + value.SearchQuery + "\n" + Details).Trim();
                    if (string.IsNullOrWhiteSpace(Details)) Details = ("Uri: " + Uri.AbsoluteUri).Trim();

                    Summary = value.Details?.Summary;

                    tags = value.Tags;
                }
                finally
                {
                    OnPropertyChanged("");
                }

            }
        }


    }
}
