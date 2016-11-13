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

    public class Ao3WorkDetails
    {
        public IReadOnlyDictionary<string,string> Authors { get; set; }
        public IReadOnlyDictionary<string, string> Recipiants { get; set; }
        public IReadOnlyDictionary<string,Tuple<int,string>> Series { get; set; }
        public string LastUpdated { get; set; }
        public int? Words { get; set; }
        public Tuple<int, int?> Chapters { get; set; }
        public int? Collections { get; set; }
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
        public string Title;        

        public Dictionary<Ao3TagType, List<string>> Tags { set; get; }

        public Dictionary<Ao3RequiredTags, Tuple<string, string>> RequiredTags { get; set; }
        public Uri GetRequiredTagsUri(Ao3RequiredTags tag) {
            Tuple<string, string> rt;
            if (RequiredTags == null || !RequiredTags.TryGetValue(tag, out rt))
                return null;

            return new Uri("https://archiveofourown.org/images/skins/iconsets/default_large/"+rt.Item1+".png");
        }
        public string GetRequiredTagsText(Ao3RequiredTags tag)
        {
            Tuple<string, string> rt;
            if (RequiredTags == null || !RequiredTags.TryGetValue(tag, out rt))
                return null;

            return rt.Item2;
        }

        public string Language { set; get; }

        public bool Complete { set; get; }

        public Ao3WorkDetails Details { get; set; }

        public string SearchQuery { get; set; }
    }
}
