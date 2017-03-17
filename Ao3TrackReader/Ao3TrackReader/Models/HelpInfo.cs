using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;

namespace Ao3TrackReader.Models
{
    public interface IHelpInfo : IGroupable, INotifyPropertyChanged
    {
        string Text { get; }
        FormattedString Description { get; }
        FileImageSource Icon { get; }
    }

    public class HelpInfo : IHelpInfo
    {
        public string Text { get; set; }
        public FormattedString Description { get; set; }
        public FileImageSource Icon { get; set; }

        public string Group { get; set; }

        public string GroupType { get; set; }

        public bool ShouldHide { get; } = false;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { } 
        }
    }

    public class HelpInfoAdapter : IHelpInfo
    {
        private IHelpInfo source;

        public HelpInfoAdapter(IHelpInfo source)
        {
            this.source = source;
        }

        public string Text => source.Text;
        public FormattedString Description => source.Description;
        public FileImageSource Icon => source.Icon;

        public string Group => source.Group;

        public string GroupType => source.GroupType;

        public bool ShouldHide => source.ShouldHide;

        public event PropertyChangedEventHandler PropertyChanged {
            add { source.PropertyChanged += value; }
            remove { source.PropertyChanged -= value; }
        }

    }


}
