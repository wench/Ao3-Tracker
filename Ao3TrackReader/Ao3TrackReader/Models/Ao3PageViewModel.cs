using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

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
        public event PropertyChangingEventHandler PropertyChanging;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanging(string propertyName)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        string title;
        public string Title
        {
            get { return title; }
        }

        string details;
        public string Detail
        {
            get { return details; }
        }

        string group;
        public string Group
        {
            get { return group; }
        }

        string uri;
        public string Uri {
            get { return uri; }
            set {
                OnPropertyChanging("Title");
                OnPropertyChanging("Detail");
                uri = value;
                title = "Loading...";
                details = uri;
                OnPropertyChanged("Title");
                OnPropertyChanged("Detail");
            }
        }

        Ao3PageModel baseData;
        public Ao3PageModel BaseData {
            get { return baseData; }
            set { baseData = value;

                // Generate everything from Ao3PageModel 

            }
        }


    }
}
