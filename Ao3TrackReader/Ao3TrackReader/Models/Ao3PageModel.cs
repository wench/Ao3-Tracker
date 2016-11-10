using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Models
{

    public enum Ao3PageType
    {
        Other,
        Work,
        Tag, 
        Search,
        Unknown
    }

    public enum Ao3TagType
    {
        Other = 0,
        Rating,
        Warnings,
        Category,
        Fandoms,
        Characters,
        Relationships,
        Freeforms
    }

    public enum Ao3RequiredTags
    {
        Rating,
        Warning,
        Category,
        Complete
    }

    public class Ao3WorkStats
    {
        public string LastUpdated { get; set; }
        public int? Words { get; set; }
        public Tuple<int, int?> Chapters { get; set; }
        public int? Comments { get; set; }
        public int? Kudos { get; set; }
        public int? Bookmarks { get; set; }
        public int? Hits { get; set; }
    }


    public class Ao3PageModel
    {
        public Uri Uri { set; get; }
        public Ao3PageType Type { set; get; }
        public string PrimaryTag { set; get; }

        public Dictionary<Ao3TagType, List<string>> Tags { set; get; }
        
        public Dictionary<Ao3RequiredTags, Tuple<string,string>> RequiredTags { get; set; }

        public string Language { set; get; }

        public bool Complete { set; get; }

        public Ao3WorkStats Stats { get; set; }

        public string SearchQuery { get; set; }
    }
}
