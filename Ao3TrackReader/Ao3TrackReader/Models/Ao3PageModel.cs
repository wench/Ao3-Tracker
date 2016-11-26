using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Models
{

    public enum Ao3PageType
    {
        Tag,
        Search,
        Work,
        Bookmarks,
        Other,
        Unknown
    }

    public enum Ao3TagType
    {
        Warnings,
        Rating,
        Category,
        Fandoms,
        Relationships,
        Characters,
        Freeforms,
        Other
    }

    public enum Ao3RequiredTag
    {
        Rating,
        Warnings,
        Category,
        Complete
    }

    public class Ao3WorkDetails
    {
        public long WorkId { get; set; }
        public IReadOnlyDictionary<string,string> Authors { get; set; }
        public IReadOnlyDictionary<string, string> Recipiants { get; set; }
        public IReadOnlyDictionary<string,Tuple<int,string>> Series { get; set; }
        public string LastUpdated { get; set; }
        public int? Words { get; set; }
        public Tuple<int?, int, int?> Chapters { get; set; }
        public int? Collections { get; set; }
        public int? Comments { get; set; }
        public int? Kudos { get; set; }
        public int? Bookmarks { get; set; }
        public int? Hits { get; set; }
        public TextTree Summary { get; set; }
    }


    public class Ao3PageModel
    {
        public Uri Uri { set; get; }
        public Ao3PageType Type { set; get; }
        public string PrimaryTag { set; get; }
        public Ao3TagType PrimaryTagType { set; get; }
        public string Title;        

        public SortedDictionary<Ao3TagType, List<string>> Tags { set; get; }

        public Dictionary<Ao3RequiredTag, Tuple<string, string>> RequiredTags { get; set; }
        public Uri GetRequiredTagUri(Ao3RequiredTag tag) {
            Tuple<string, string> rt = null;
            if (RequiredTags == null || !RequiredTags.TryGetValue(tag, out rt) || rt == null)
            {
                if (tag == Ao3RequiredTag.Category) rt = new Tuple<string, string>("category-none", "None");
                else if (tag == Ao3RequiredTag.Complete) rt = new Tuple<string, string>("category-none", "None");
                else if (tag == Ao3RequiredTag.Rating) rt = new Tuple<string, string>("rating-notrated", "None");
                else if (tag == Ao3RequiredTag.Warnings) rt = new Tuple<string, string>("warning-none", "None");
            }

            return new Uri("https://archiveofourown.org/images/skins/iconsets/default_large/"+rt.Item1+".png");
        }
        public string GetRequiredTagText(Ao3RequiredTag tag)
        {
            Tuple<string, string> rt;
            if (RequiredTags == null || !RequiredTags.TryGetValue(tag, out rt))
                return null;

            return rt.Item2;
        }

        public string Language { set; get; }

        public Ao3WorkDetails Details { get; set; }

        public string SearchQuery { get; set; }
    }
}
