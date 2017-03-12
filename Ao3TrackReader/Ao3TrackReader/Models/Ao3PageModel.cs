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
using System.Text;

namespace Ao3TrackReader.Models
{

    public enum Ao3PageType
    {
        Tag,
        Search,
        Work,
        Bookmarks,
        Series,
        Collection,
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

    public class Ao3ChapterDetails
    {
        public Ao3ChapterDetails(int available, int? total) { Available = available; Total = total; }
        public int Available { get; set; }
        public int? Total { get; set; }
    }
    public class Ao3SeriesLink
    {
        public Ao3SeriesLink(long seriesid, string url) { SeriesId = seriesid; Url = url; }
        public long SeriesId { get; set; }
        public string Url { get; set; }
    }

    public class Ao3WorkDetails
    {
        public long WorkId { get; set; }
        public IReadOnlyDictionary<string,string> Authors { get; set; }
        public IReadOnlyDictionary<string, string> Recipiants { get; set; }
        public IReadOnlyDictionary<string, Ao3SeriesLink> Series { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int? Words { get; set; }
        public Ao3ChapterDetails Chapters { get; set; }
        public bool? IsComplete { get { return Chapters != null ? Chapters.Available == Chapters.Total : (bool?)null; } }
        public int? Collections { get; set; }
        public int? Comments { get; set; }
        public int? Kudos { get; set; }
        public int? Bookmarks { get; set; }
        public int? Hits { get; set; }
        public int? Works { get; set; }
        public TextTree Summary { get; set; }
    }

    public class Ao3RequredTagData
    {
        public Ao3RequredTagData(string tag, string label) { Tag = tag; Label = label; }
        public string Tag { get; private set; }
        public string Label { get; private set; }
    }


    public class Ao3PageModel
    {
        public Uri Uri { set; get; }
        public Ao3PageType Type { set; get; }
        public string PrimaryTag { set; get; }
        public Ao3TagType PrimaryTagType { set; get; }
        public string Title;        

        public SortedDictionary<Ao3TagType, List<string>> Tags { set; get; }

        public Dictionary<Ao3RequiredTag, Ao3RequredTagData> RequiredTags { get; set; }
        public Uri GetRequiredTagUri(Ao3RequiredTag tag) {
            Ao3RequredTagData rt = null;

            if (RequiredTags == null || RequiredTags.Count == 0)
                return null;

            if (!RequiredTags.TryGetValue(tag, out rt) || rt == null)
            {
                if (tag == Ao3RequiredTag.Category) rt = new Ao3RequredTagData("category-none", "None");
                else if (tag == Ao3RequiredTag.Complete) rt = new Ao3RequredTagData("category-none", "None");
                else if (tag == Ao3RequiredTag.Rating) rt = new Ao3RequredTagData("rating-notrated", "None");
                else if (tag == Ao3RequiredTag.Warnings) rt = new Ao3RequredTagData("warning-none", "None");
            }

            return new Uri("http://archiveofourown.org/images/skins/iconsets/default_large/"+rt.Tag+".png");
        }
        public string GetRequiredTagText(Ao3RequiredTag tag)
        {
            Ao3RequredTagData rt;
            if (RequiredTags == null || !RequiredTags.TryGetValue(tag, out rt))
                return null;

            return rt.Label;
        }

        public string Language { set; get; }

        public Ao3WorkDetails Details { get; set; }

        public string SearchQuery { get; set; }

        public string SortColumn { get; set; }
        public string SortDirection { get; set; }

        public IReadOnlyCollection<Ao3PageModel> SeriesWorks { get; set; }
    }
}
