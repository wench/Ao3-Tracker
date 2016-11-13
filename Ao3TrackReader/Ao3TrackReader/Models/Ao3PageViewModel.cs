using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;

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

        public string Group { get; private set; }
        public string Title { get; private set; }
        public string Date { get; private set; }


        private Uri imageRatingUri;
        public ImageSource ImageRating { get { return new UriImageSource { Uri = imageRatingUri }; } }

        private Uri imageWarningsUri;
        public ImageSource ImageWarnings { get { return new UriImageSource { Uri = imageWarningsUri }; } }

        private Uri imageCategoryUri;
        public ImageSource ImageCategory { get { return new UriImageSource { Uri = imageCategoryUri }; } }

        private Uri imageCompleteUri;
        public ImageSource ImageComplete { get { return new UriImageSource { Uri = imageCompleteUri }; } }

        List<string> detail;
        public FormattedString Detail
        {
            get
            {
                var fs = new FormattedString();
                foreach (var s in detail) fs.Spans.Add(new Span { Text = s });
                return fs;
            }
        }

        public Uri Uri { get; private set; }

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

                Uri image;
                if ((image = value.GetRequiredTagsUri(Ao3RequiredTags.Rating)) != null || imageRatingUri != null)
                {
                    OnPropertyChanging("ImageRating");
                    imageRatingUri = image;
                    OnPropertyChanged("ImageRating");
                }

                if ((image = value.GetRequiredTagsUri(Ao3RequiredTags.Warning)) != null || imageWarningsUri != null)
                {
                    OnPropertyChanging("ImageWarnings");
                    imageWarningsUri = image;
                    OnPropertyChanged("ImageWarnings");
                }

                if ((image = value.GetRequiredTagsUri(Ao3RequiredTags.Category)) != null || imageCategoryUri != null)
                {
                    OnPropertyChanging("ImageCategory");
                    imageCategoryUri = image;
                    OnPropertyChanged("ImageCategory");
                }

                if ((image = value.GetRequiredTagsUri(Ao3RequiredTags.Complete)) != null || imageCompleteUri != null)
                {
                    OnPropertyChanging("ImageComplete");
                    imageCompleteUri = image;
                    OnPropertyChanged("ImageComplete");
                }

                OnPropertyChanging("Uri");
                Uri = value.Uri;
                OnPropertyChanged("Uri");

                OnPropertyChanging("Title");
                Title = value.Title;
                OnPropertyChanged("Title");

                OnPropertyChanging("Date");
                Date = value.Details?.LastUpdated;
                OnPropertyChanged("Date");

                OnPropertyChanging("Detail");
                detail = new List<string> {
                    value.Uri.AbsoluteUri
                };
                OnPropertyChanged("Detail");
            }
        }


    }
}
