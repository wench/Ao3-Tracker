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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.Reflection;

namespace Ao3TrackReader.Models
{

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Ao3PageType
    {
        Unknown = 0,
        Tag,
        Search,
        Work,
        Bookmarks,
        Series,
        Collection,
        Other
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Ao3TagType
    {
        Unknown = 0,
        Warnings,
        Rating,
        Category,
        Complete,
        Fandoms,
        Relationships,
        Characters,
        Freeforms,
        Other
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Ao3RequiredTag
    {
        Rating,
        Warnings,
        Category,
        Complete
    }

    public class Ao3ChapterDetails
    {
        public Ao3ChapterDetails() { }
        public Ao3ChapterDetails(int available, int? total) { Available = available; Total = total; }

        public int Available { get; set; }
        public int? Total { get; set; }
    }
    public class Ao3SeriesLink
    {
        public Ao3SeriesLink() { }
        public Ao3SeriesLink(long seriesid, string url) { SeriesId = seriesid; Url = url; }

        public long SeriesId { get; set; }
        public string Url { get; set; }
    }

    public class Ao3WorkDetails
    {
        public Ao3WorkDetails() { }

        public long WorkId { get; set; }
        public Dictionary<string,string> Authors { get; set; }
        public Dictionary<string, string> Recipiants { get; set; }
        public Dictionary<string, Ao3SeriesLink> Series { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int? Words { get; set; }
        public Ao3ChapterDetails Chapters { get; set; }
        public int? Collections { get; set; }
        public int? Comments { get; set; }
        public int? Kudos { get; set; }
        public int? Bookmarks { get; set; }
        public int? Hits { get; set; }
        public int? Works { get; set; }
        public Text.TextEx Summary { get; set; }

        [JsonIgnore]
        public bool? IsComplete { get { return Chapters != null ? Chapters.Available == Chapters.Total : (bool?)null; } }
    }

    public class Ao3RequredTagData
    {
        public Ao3RequredTagData() { }
        public Ao3RequredTagData(string tag, string label) { Tag = tag; Label = label; }

        private string tag;
        public string Tag { get => tag; set => tag = value.PoolString(); }

        private string label;
        public string Label { get => label; set => label = value.PoolString(); }
    }


    public class Ao3PageModel
    {
        public Ao3PageModel() {  }

        public Uri Uri { set; get; }
        public Ao3PageType Type { set; get; }
        private string primaryTag;
        public string PrimaryTag { set => primaryTag = value.PoolString(); get => primaryTag; }
        public Ao3TagType PrimaryTagType { set; get; }
        public string Title;        

        public SortedDictionary<Ao3TagType, List<string>> Tags { set; get; }

        public Dictionary<Ao3RequiredTag, Ao3RequredTagData> RequiredTags { get; set; }
        private static Dictionary<string, Uri> requiredTagUris = new Dictionary<string, Uri>(16);
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

            lock (requiredTagUris)
            {
                if (requiredTagUris.TryGetValue(rt.Tag, out var result))
                    return result;

                return requiredTagUris[rt.Tag] = new Uri("https://archiveofourown.org/images/skins/iconsets/default_large/" + rt.Tag + ".png");
            }
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

        [JsonIgnore]
        public bool HasChapters => Type == Ao3PageType.Series || Type == Ao3PageType.Work || Type == Ao3PageType.Collection;

        private class SerializationBinder : Newtonsoft.Json.Serialization.DefaultSerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (assemblyName.StartsWith("Ao3TrackReader", StringComparison.OrdinalIgnoreCase))
                    assemblyName = GetType().GetTypeInfo().Assembly.FullName;

                return base.BindToType(assemblyName, typeName);
            }
        }


        static Newtonsoft.Json.JsonSerializerSettings JsonSettings { get; } = new Newtonsoft.Json.JsonSerializerSettings()
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Auto,
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple,
            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat,
            DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
            MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            StringEscapeHandling = Newtonsoft.Json.StringEscapeHandling.Default,
            Formatting = Newtonsoft.Json.Formatting.None,
            SerializationBinder = new SerializationBinder(),
        };

        string serialized = null;
        public static string Serialize(Ao3PageModel model)
        {
            if (model == null) return null;
            if (!string.IsNullOrWhiteSpace(model.serialized)) return model.serialized;
            return model.serialized = Newtonsoft.Json.JsonConvert.SerializeObject(model, JsonSettings);
        }
        public static Ao3PageModel Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            var model = Newtonsoft.Json.JsonConvert.DeserializeObject<Ao3PageModel>(json, JsonSettings);
            model.serialized = json;
            return model;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Tags != null) {
                foreach (var list in Tags.Values) {
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i] = list[i].PoolString();
                    }
                }
            }
        }
    }
}
