using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;
using System.Linq;

namespace Ao3TrackReader.Models
{
    // How this stuff is intended to be formatted
    //
    // A work:
    // <ICON> <Title> by <User> (for <User>)
    // <ICON>  


    public class Ao3PageViewModel : IGroupable, INotifyPropertyChanged, INotifyPropertyChanging

    {
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
        public string Title { get; private set; }
        public string Date { get; private set; }
        public string Subtitle { get; private set; }
        public string Details { get; private set; }


        private Uri imageRatingUri;
        public ImageSource ImageRating { get { return new UriImageSource { Uri = imageRatingUri }; } }

        private Uri imageWarningsUri;
        public ImageSource ImageWarnings { get { return new UriImageSource { Uri = imageWarningsUri }; } }

        private Uri imageCategoryUri;
        public ImageSource ImageCategory { get { return new UriImageSource { Uri = imageCategoryUri }; } }

        private Uri imageCompleteUri;
        public ImageSource ImageComplete { get { return new UriImageSource { Uri = imageCompleteUri }; } }

        SortedDictionary<Ao3TagType,List<string>> tags;
        public FormattedString Tags
        {
            get
            {
                var fs = new FormattedString();

                if (tags != null)
                {
                    foreach (var t in tags)
                    {
                        if (t.Key == Ao3TagType.Fandoms) continue;

                        FontAttributes attribs = FontAttributes.None;
                        Color color;
                        if (t.Key == Ao3TagType.Warnings)
                        {
                            color = App.Colors["SystemBaseHighColor"];
                            attribs = FontAttributes.Bold;
                        }
                        else if (t.Key == Ao3TagType.Relationships)
                            color = App.Colors["SystemChromeAltLowColor"];
                        else if (t.Key == Ao3TagType.Characters)
                        {
                            var c1 = App.Colors["SystemChromeAltLowColor"];
                            var c2 = App.Colors["SystemChromeHighColor"];
                            color = new Color((c1.R + c2.R) / 2, (c1.G + c2.G) / 2, (c1.B + c2.B) / 2);
                        }
                        else
                            color = App.Colors["SystemChromeHighColor"];

                        foreach (var tag in t.Value)
                        {
                            fs.Spans.Add(new Span { Text = tag.Replace(' ', '\xA0'), FontSize = 10, FontAttributes = attribs, ForegroundColor = color });
                            fs.Spans.Add(new Span { Text = ",  ", FontSize = 10, FontAttributes = attribs, ForegroundColor = color });
                        }
                    }
                    if (fs.Spans.Count > 1) fs.Spans.RemoveAt(fs.Spans.Count - 1);
                }
                return fs;
            }
        }


        Ao3PageModel baseData;
        public Ao3PageModel BaseData {
            get { return baseData; }
            set {
                if (baseData == value) return;
                baseData = value;

                // Generate everything from Ao3PageModel 
                string newGroup = value.PrimaryTag ?? "";
                if (Group != newGroup)
                {
                    OnPropertyChanging("Group");
                    Group = newGroup;
                    OnPropertyChanged("Group");
                }

                if (!string.IsNullOrWhiteSpace(newGroup))
                {
                    string newgrouptype = value.PrimaryTagType.ToString().TrimEnd('s');
                    if (newgrouptype != GroupType)
                    {
                        OnPropertyChanging("GroupType");
                        GroupType = newgrouptype;
                        OnPropertyChanged("GroupType");
                    }
                }

                Uri image;
                if ((image = value.GetRequiredTagUri(Ao3RequiredTag.Rating)) != null || imageRatingUri != null)
                {
                    OnPropertyChanging("ImageRating");
                    imageRatingUri = image;
                    OnPropertyChanged("ImageRating");
                }

                if ((image = value.GetRequiredTagUri(Ao3RequiredTag.Warnings)) != null || imageWarningsUri != null)
                {
                    OnPropertyChanging("ImageWarnings");
                    imageWarningsUri = image;
                    OnPropertyChanged("ImageWarnings");
                }

                if ((image = value.GetRequiredTagUri(Ao3RequiredTag.Category)) != null || imageCategoryUri != null)
                {
                    OnPropertyChanging("ImageCategory");
                    imageCategoryUri = image;
                    OnPropertyChanged("ImageCategory");
                }

                if ((image = value.GetRequiredTagUri(Ao3RequiredTag.Complete)) != null || imageCompleteUri != null)
                {
                    OnPropertyChanging("ImageComplete");
                    imageCompleteUri = image;
                    OnPropertyChanged("ImageComplete");
                }

                OnPropertyChanging("Uri");
                Uri = value.Uri;
                OnPropertyChanged("Uri");

                OnPropertyChanging("Title");
                Title = value.Title ?? "";
                if (value.Details?.Authors != null && value.Details.Authors.Count != 0)
                {
                    Title += " by " + string.Join(",  ", value.Details.Authors.Values);
                }
                if (value.Details?.Recipiants != null && value.Details.Recipiants.Count != 0)
                {
                    Title += " for " + string.Join(",  ", value.Details.Recipiants.Values);
                }
                Title = Title.Trim();
                if (Title == "") Title = Uri.PathAndQuery;
                OnPropertyChanged("Title");

                OnPropertyChanging("Date");
                Date = value.Details?.LastUpdated ?? "";
                OnPropertyChanged("Date");

                OnPropertyChanging("Subtitle");
                Subtitle = "";
                if (value.Tags != null && value.Tags.ContainsKey(Ao3TagType.Fandoms)) Subtitle = string.Join(",  ", value.Tags[Ao3TagType.Fandoms].Select((s) => s.Replace(' ', '\xA0')));
                OnPropertyChanged("Subtitle");

                OnPropertyChanging("Details");
                List<string> d = new List<string>();
                if (!string.IsNullOrWhiteSpace(value.Language)) d.Add("Language: " + value.Language);
                if (value.Details != null)
                {
                    if (value.Details.Words != null) d.Add("Words:\xA0" + value.Details.Words.ToString());
                    if (value.Details.Chapters != null) d.Add("Chapters:\xA0" + value.Details.Chapters.Item1.ToString() + "/" + (value.Details.Chapters.Item2?.ToString() ?? "?"));
                    if (value.Details.Collections != null) d.Add("Collections:\xA0" + value.Details.Collections.ToString());
                    if (value.Details.Comments != null) d.Add("Comments:\xA0" + value.Details.Comments.ToString());
                    if (value.Details.Kudos != null) d.Add("Kudos:\xA0" + value.Details.Kudos.ToString());
                    if (value.Details.Bookmarks != null) d.Add("Bookmarks:\xA0" + value.Details.Bookmarks.ToString());
                    if (value.Details.Hits != null) d.Add("Hits:\xA0" + value.Details.Hits.ToString());

                }

                Details = string.Join("   ", d);
                if (!string.IsNullOrWhiteSpace(value.SearchQuery)) Details = ("Query: " + value.SearchQuery + "\n" + Details).Trim();
                OnPropertyChanged("Details");

                OnPropertyChanging("Tags");
                tags = value.Tags;
                OnPropertyChanged("Tags");
            }
        }


    }
}
