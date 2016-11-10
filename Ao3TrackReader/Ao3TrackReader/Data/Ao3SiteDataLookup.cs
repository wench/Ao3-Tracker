using System;
using System.Collections.Generic;
using System.Text;
using Ao3TrackReader.Models;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Xml;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Ao3TrackReader.Data
{
    // Data Grabbing from Ao3 itself or from URLs
    //
    // * For work: Get details from summary on a search page https://archiveofourown.org/works/search?utf8=%E2%9C%93&work_search%5Bquery%5D=id%3A(<<WORKIDS>>) where <<WORKIDS>> = number | number+OR+<<WORKIDS>>
    // * A tag page: https://archiveofourown.org/tags/<<TAGNAME>>[''|'/works'|'/bookmarks']
    // * in searches: https://archiveofourown.org/works?tag_id=<<TAGNAME>> and details from the rest of the crazy long query string
    // 
    // * series: details from series page https://archiveofourown.org/series/<<SERIESID>>  coalate fandoms and relationship tags

    // Tag names must not have any of ,^*<>{}`\%=
    // In urls these substitutions apply:
    // *s* = /
    // *a* = &
    // *d* = .
    // *q* = ?
    // *h* = #

    // Content Ratings TL
    // [G Green] General Audiences  .rating-general-audience 
    // [T Yellow] Teen and Up  .rating-teen 
    // [M Orange] Mature .rating-mature 
    // [E Red] Explicit  .rating-explicit 
    // [] No rating given .rating-notrated
    //
    // Relationships, pairings, orientations TR
    // [♀ Red]         .category-femslash 
    // [♀♂ Purple]     .category-het 
    // [O. Green]      .category-gen 
    // [♂ Blue]        .category-slash 
    // [GP|RB] Multi   .category-multi 
    // [? Black] Other .category-other 
    // [] No category set .category-none
    //
    // Content warnings BL
    // [!? Orange] Author chose not to warn .warning-choosenotto
    // [! Red] A warning applies     .warning-yes 
    // [] Not marked with a warning  .warning-no
    // [Earth Blue] External work    .external-work 
    //
    // Finished BR
    // [O\ Red] Incomplete  .complete-no 
    // [v/ Green] Complete  .complete-yes 
    // [] Unknown   
    //
    // Get icons from https://archiveofourown.org/images/skins/iconsets/default_large/<<CLASSNAME>>.png

    public static class HtmlAgilitiyExtension
    {
        public static string[] GetClasses(this HtmlNode node)
        {
            var attrClass = node.Attributes["class"];
            if (attrClass != null && !string.IsNullOrEmpty(attrClass.Value))
                return attrClass.Value.Split(' ');
            return new string[] { };
        }

        public static bool HasClass(this HtmlNode node, string classname)
        {
            return Array.IndexOf(node.GetClasses(), classname) != -1;
        }

        public static HtmlNode ElementByClass(this HtmlNode node, string classname)
        {
            foreach (var e in node.ChildNodes)
            {
                if (e.NodeType != HtmlNodeType.Element) continue;

                if (e.HasClass(classname)) return e;
            }
            return null;
        }

        public static HtmlNode ElementByClass(this HtmlNode node, string tagname, string classname)
        {
            foreach (var e in node.Elements(tagname))
            {
                if (e.HasClass(classname)) return e;
            }
            return null;
        }

        public static IEnumerable<HtmlNode> ElementsByClass(this HtmlNode node, string classname)
        {
            foreach (var e in node.ChildNodes)
            {
                if (e.NodeType != HtmlNodeType.Element) continue;
                if (e.HasClass(classname)) yield return e;
            }
        }

        public static IEnumerable<HtmlNode> ElementsByClass(this HtmlNode node, string tagname, string classname)
        {
            foreach (var e in node.Elements(tagname))
            {
                if (e.HasClass(classname)) yield return e;
            }
        }

        public static IEnumerable<HtmlNode> DescendantsByClass(this HtmlNode node, string tagname, string classname)
        {
            foreach (var e in node.Descendants(tagname))
            {
                if (e.HasClass(classname)) yield return e;
            }
        }
    }


    public class Ao3SiteDataLookup
    {
        static Regex regexEscTag = new Regex(@"([/&.?#])");
        static Regex regexUnescTag = new Regex(@"(\*[sadqh])\*");
        static Regex regexTag = new Regex(@"^/tags/(?<TAGNAME>[^/&.?#,^<>{}`\\=]+)(/(?<TYPE>(works|bookmarks)?))?$", RegexOptions.ExplicitCapture);
        static Regex regexWork = new Regex(@"^/works/(?<WORKID>\d+)(/chapter/(?<CHAPTERID>\d+))?$", RegexOptions.ExplicitCapture);
        static Regex regexRSSTagTitle = new Regex(@"'(?<TAGNAME>[^,^*<>{}`\%=]*)'", RegexOptions.ExplicitCapture);
        static Regex regexTagCategory = new Regex(@"This tag belongs to the (?<CATEGORY>\w*) Category\.", RegexOptions.ExplicitCapture);

        static string EscapeTag(string tag)
        {
            return regexEscTag.Replace(tag, (match) =>
            {
                switch (match.Value)
                {
                    case "/":
                        return "*s*";

                    case "&":
                        return "*a*";

                    case ".":
                        return "*d*";

                    case "?":
                        return "*q*";

                    case "#":
                        return "*h*";
                }
                return "";
            });
        }

        static string UnescapeTag(string tag)
        {
            return regexUnescTag.Replace(tag, (match) =>
            {
                switch (match.Value)
                {
                    case "*s*":
                        return "/";

                    case "*a*":
                        return "&";

                    case "*d*":
                        return ".";

                    case "*q*":
                        return "?";

                    case "*h*":
                        return "#";
                }
                return "";
            });
        }

        static async Task<string> LookupTag(int tagid)
        {
            string tag = App.Database.GetTagForId(tagid);
            if (!string.IsNullOrEmpty(tag))
                return UnescapeTag(tag);

            tag = null;
            var uri = new Uri(@"https://archiveofourown.org/tags/feed/" + tagid.ToString());

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get,uri);
            message.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/atom+xml"));
            var response = await App.HttpClient.SendAsync(message, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);

            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                Uri newuri = response.Headers.Location;
                var m = regexTag.Match(newuri.LocalPath);
                if (m.Success)
                {
                    tag = UnescapeTag(m.Groups["TAGNAME"].Value);
                }
            }
            else if (response.IsSuccessStatusCode)
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;
                settings.IgnoreComments = true;

                using (var xml = XmlReader.Create(await response.Content.ReadAsStreamAsync(), settings))
                {
                    xml.MoveToContent();
                    if (!xml.ReadToDescendant("title")) return null;

                    try {
                        xml.Read();

                        var m = regexRSSTagTitle.Match(xml.ReadContentAsString());
                        if (m.Success)
                        {
                            tag = m.Groups["TAGNAME"].Value;
                        }
                    }
                    catch {

                    }

                }
            }

            App.Database.SetTagId(EscapeTag(tag), tagid);
            return UnescapeTag(tag);
        }

        static async Task<string> LookupTag(string intag)
        {
            intag = EscapeTag(intag);

            string tag = App.Database.GetTagMerger(intag);
            if (!string.IsNullOrEmpty(tag))
            {
                return UnescapeTag(tag);
            }
            tag = intag;

            var uri = new Uri(@"https://archiveofourown.org/tags/" + intag);

            var response = await App.HttpClient.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                HtmlDocument doc = new HtmlDocument();
                Encoding encoding = Encoding.UTF8;
                try
                {
                    encoding = Encoding.GetEncoding(response.Content.Headers.ContentType.CharSet);
                }
                catch
                {
                }
                doc.Load(await response.Content.ReadAsStreamAsync(), encoding);
                var main = doc.GetElementbyId("main");

                HtmlNode tagnode = main.ElementByClass("div", "tag");
                if (tagnode != null)
                {
                    // Category
                    foreach (var p in tagnode.Elements("p"))
                    {
                        if (p.InnerText != null)
                        {
                            var m = regexTagCategory.Match(p.InnerText);
                            if (m.Success)
                            {
                                App.Database.SetTagCategory(intag, m.Groups["CATEGORY"].Value);
                                break;
                            }
                        }
                    }

                    // Merger?
                    HtmlNode mergernode = tagnode.ElementByClass("div", "merger");
                    if (mergernode != null)
                    {
                        foreach (var e in mergernode.DescendantsByClass("a", "tag"))
                        {
                            var href = e.Attributes["href"];
                            if (href != null && !string.IsNullOrEmpty(href.Value))
                            {
                                var newuri = new Uri(uri, href.Value);
                                var m = regexTag.Match(newuri.LocalPath);
                                if (m.Success) tag = m.Groups["TAGNAME"].Value;
                                break;
                            }
                        }
                    }
                }

            }

            App.Database.SetTagMerger(intag, EscapeTag(tag));
            return UnescapeTag(tag);
        }

        static async Task<Ao3TagType> LookupTagCategory(string tag)
        {
            tag = EscapeTag(tag);
            await LookupTag(tag);

            string category = App.Database.GetTagCategory(tag);
            if (!string.IsNullOrEmpty(category))
            {
                Ao3TagType type;
                if (Enum.TryParse(category, true, out type) || Enum.TryParse(category + "s", true, out type))
                    return type;
            }

            return Ao3TagType.Other;
        }

        static async Task<string> LookupLanguage(int langid)
        {
            string name = App.Database.GetLanguage(langid);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            var uri = new Uri(@"https://archiveofourown.org/works/search");

            var response = await App.HttpClient.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                HtmlDocument doc = new HtmlDocument();
                Encoding encoding = Encoding.UTF8;
                try
                {
                    encoding = Encoding.GetEncoding(response.Content.Headers.ContentType.CharSet);
                }
                catch
                {
                }
                HtmlNode.ElementsFlags["option"] = HtmlElementFlag.Empty | HtmlElementFlag.Closed;
                doc.Load(await response.Content.ReadAsStreamAsync(), encoding);
                var langselect = doc.GetElementbyId("work_search_language_id");
                foreach (var opt in langselect.Elements("option"))
                {
                    var value = opt.Attributes["value"];

                    if (value != null && !string.IsNullOrEmpty(value.Value) && !string.IsNullOrWhiteSpace(opt.InnerText))
                    {
                        int i;
                        if (int.TryParse(value.Value, out i))
                        {
                            var n = opt.InnerText.Trim();
                            App.Database.SetLanguage(n, i);
                            if (i == langid) name = n;
                        }
                    }

                }

            }

            return name;
        }


        public static async Task<IDictionary<string, Ao3PageModel>> Lookup(ICollection<string> urls)
        {
            var tasks = new List<Task<KeyValuePair<string, Ao3PageModel>>>(urls.Count);
            
            foreach (string url in urls)
            { 
                tasks.Add(Task.Run(async () => {
                    var uri = new UriBuilder(url);

                    if (uri.Host == "archiveofourown.org" || uri.Host == "www.archiveofourown.org")
                    {
                        if (uri.Scheme == "http")
                        {
                            uri.Scheme = "https";
                        }
                        uri.Port = -1;
                    }
                    else
                    {
                        return new KeyValuePair<string, Ao3PageModel>(url, null);
                    }

                    Ao3PageModel model = new Ao3PageModel
                    {
                        Uri = uri.Uri
                    };

                    Match match = null;

                    if (uri.Path == "/works" || uri.Path == "/works/search") // Work search and Advanced search
                    {
                        model.Type = Ao3PageType.Search;

                        await FillModelFromSearchQuery(uri.Uri, model);
                    }
                    else if ((match = regexTag.Match(uri.Path)).Success)    // View tag
                    {
                        model.Type = Ao3PageType.Tag;

                        var sTAGNAME = match.Groups["TAGNAME"].Value;
                        var sTYPE = match.Groups["TYPE"].Value;

                        if (sTYPE == "works")
                        {
                            model.Type = Ao3PageType.Search;
                        }

                        model.PrimaryTag = await LookupTag(sTAGNAME);

                    }
                    else if ((match = regexWork.Match(uri.Path)).Success)   // View Work
                    {
                        model.Type = Ao3PageType.Work;

                        var sWORKID = match.Groups["WORKID"].Value;

                        var wsuri = new Uri(@"https://archiveofourown.org/works/search?utf8=%E2%9C%93&work_search%5Bquery%5D=id%3A" + sWORKID);
                        var response = await App.HttpClient.GetAsync(wsuri);

                        if(response.IsSuccessStatusCode)
                        {

                            HtmlDocument doc = new HtmlDocument();
                            Encoding encoding = Encoding.UTF8;
                            try { 
                                encoding = Encoding.GetEncoding(response.Content.Headers.ContentType.CharSet);
                            }
                            catch {
                            }
                            doc.Load(await response.Content.ReadAsStreamAsync(), encoding);

                            var worknode = doc.GetElementbyId("work_" + sWORKID);

                            await FillModelFromWorkSummary(wsuri, worknode, model);
                        }
                    }
                    else
                    {
                        model.Type = Ao3PageType.Unknown;
                    }
                    return new KeyValuePair<string, Ao3PageModel>(url, model);
                }));
            }

            var dict = new Dictionary<string, Ao3PageModel>(urls.Count);
            foreach (var kp in await Task.WhenAll(tasks))
            {
                dict[kp.Key] = kp.Value;
            }
            return dict;
        }

        private static async Task FillModelFromWorkSummary(Uri baseuri, HtmlNode worknode, Ao3PageModel model)
        {
            // Gather all tags
            Dictionary<Ao3TagType, List<string>> tags = new Dictionary<Ao3TagType, List<string>>();

            // Tags node
            HtmlNode tagsnode = worknode.ElementByClass("ul", "tags");

            if (tagsnode != null)
            {
                foreach (var tn in tagsnode.Elements("li"))
                {
                    var a = tn.DescendantsByClass("a", "tag").FirstOrDefault();
                    if (a == null) continue;

                    Ao3TagType type = Ao3TagType.Other;

                    foreach (var c in tn.GetClasses())
                    {
                        if (Enum.TryParse(c, true, out type))
                        {
                            break;
                        }
                    }

                    var href = a.Attributes["href"];
                    if (href != null && !string.IsNullOrEmpty(href.Value))
                    {
                        var reluri = new Uri(baseuri, a.Attributes["href"].Value);
                        var m = regexTag.Match(reluri.LocalPath);
                        if (m.Success)
                        {
                            var tag = m.Groups["TAGNAME"].Value;

                            List<string> list;
                            if (!tags.TryGetValue(type, out list)) tags[type] = list = new List<string>();
                            list.Add(UnescapeTag(tag));
                        }
                    }
                }
            }

            // Header
            var headernode = worknode.ElementByClass("div", "header");

            // Get Fandom tags
            HtmlNode fandomnode = headernode?.ElementByClass("fandoms");
            if (fandomnode != null)
            {
                foreach (var a in fandomnode.Elements("a"))
                {
                    var href = a.Attributes["href"];
                    if (href != null && !string.IsNullOrEmpty(href.Value))
                    {
                        var reluri = new Uri(baseuri, a.Attributes["href"].Value);
                        var m = regexTag.Match(reluri.LocalPath);
                        if (m.Success)
                        {
                            var tag = m.Groups["TAGNAME"].Value;
                            List<string> list;
                            if (!tags.TryGetValue(Ao3TagType.Fandoms, out list)) tags[Ao3TagType.Fandoms] = list = new List<string>();
                            list.Add(UnescapeTag(tag));

                        }
                    }
                }
            }

            // Get requried tags
            HtmlNode requirednode = headernode?.ElementByClass("required-tags");
            Dictionary<Ao3RequiredTags, HtmlNode> required = new Dictionary<Ao3RequiredTags, HtmlNode>(4);
            required[Ao3RequiredTags.Rating] = requirednode?.DescendantsByClass("span", "rating")?.FirstOrDefault();
            required[Ao3RequiredTags.Warning] = requirednode?.DescendantsByClass("span", "warnings")?.FirstOrDefault();
            required[Ao3RequiredTags.Category] = requirednode?.DescendantsByClass("span", "category")?.FirstOrDefault();
            required[Ao3RequiredTags.Complete] = requirednode?.DescendantsByClass("span", "iswip")?.FirstOrDefault();

            model.RequiredTags = new Dictionary<Ao3RequiredTags, Tuple<string, string>>(4);
            foreach (var n in required)
            {
                if (n.Value == null)
                {
                    model.RequiredTags[n.Key] = null;
                    continue;
                }

                var classes = n.Value.GetClasses();
                var search = n.Key.ToString() + "-";
                var tag = Array.Find(classes, (val) => {
                    return val.StartsWith(search, StringComparison.OrdinalIgnoreCase);
                });

                if (tag == null)
                    model.RequiredTags[n.Key] = null;
                else
                    model.RequiredTags[n.Key] = new Tuple<string, string>(tag, n.Value.InnerText.Trim());

            }

            model.PrimaryTag = null;
            model.Tags = tags;

            // Get primary tag... 
            if (tags.ContainsKey(Ao3TagType.Relationships) && tags[Ao3TagType.Relationships].Count > 0) model.PrimaryTag = tags[Ao3TagType.Relationships][0];
            else if (tags.ContainsKey(Ao3TagType.Fandoms) && tags[Ao3TagType.Fandoms].Count > 0) model.PrimaryTag = tags[Ao3TagType.Fandoms][0];

            if (model.PrimaryTag != null)
            {
                model.PrimaryTag = await LookupTag(model.PrimaryTag);
            }

            // Stats

            var stats = worknode.ElementByClass("dl", "stats");
            model.Stats = new Ao3WorkStats();

            model.Stats.LastUpdated = headernode?.ElementByClass("p", "datetime")?.InnerText?.Trim();

            model.Language = stats?.ElementByClass("dd", "language")?.InnerText?.Trim();

            try { 
                model.Stats.Words = int.Parse(stats?.ElementByClass("dd", "words")?.InnerText?.Replace(",",""));
            }
            catch
            {

            }

            var chapters = stats?.ElementByClass("dd", "chapters")?.InnerText?.Trim()?.Split('/');
            if (chapters != null)
            {
                try
                {
                    int? total;
                    if (chapters[1] == "?") total = null;
                    else total = int.Parse(chapters[1]);
                    model.Stats.Chapters = new Tuple<int, int?>(int.Parse(chapters[0]), total);
                    model.Complete = model.Stats.Chapters.Item1 == model.Stats.Chapters.Item2;
                }
                catch
                {

                }
            }

            try
            {
                model.Stats.Comments = int.Parse(stats?.ElementByClass("dd", "comments")?.InnerText);
            }
            catch
            {

            }

            try
            {
                model.Stats.Kudos = int.Parse(stats?.ElementByClass("dd", "kudos")?.InnerText);
            }
            catch
            {

            }

            try
            {
                model.Stats.Bookmarks = int.Parse(stats?.ElementByClass("dd", "bookmarks")?.InnerText);
            }
            catch
            {

            }

            try
            {
                model.Stats.Hits = int.Parse(stats?.ElementByClass("dd", "hits")?.InnerText);
            }
            catch
            {

            }
        }

        private static async Task FillModelFromSearchQuery(Uri uri, Ao3PageModel model)
        {
            Dictionary<string, List<string>> query = new Dictionary<string, List<string>>();

            foreach (var v in uri.Query.TrimStart('?').Split(';', '&'))
            {
                var kv = v.Split(new[] { '=' }, 2);
                kv[0] = WebUtility.UrlDecode(kv[0]);

                if (!query.ContainsKey(kv[0]))
                {
                    query.Add(kv[0], new List<string>());
                }
                if (kv.Length == 2)
                    query[kv[0]].Add(WebUtility.UrlDecode(kv[1]));
            }


            var tasks = new List<Task<KeyValuePair<Ao3TagType, string>>>();

            foreach (var i in Enum.GetValues(typeof(Ao3TagType)))
            {
                List<string> tagids;
                var name = "work_search[" + i.ToString().ToLowerInvariant().TrimEnd('s') + "_ids][]";
                if (query.TryGetValue(name, out tagids))
                {
                    foreach(var s in tagids)
                    {
                        int id;
                        if (!int.TryParse(s, out id)) continue;

                        tasks.Add(Task.Run(async () => {
                            string tag = await LookupTag(id);
                            return new KeyValuePair<Ao3TagType, string>((Ao3TagType)i, tag);
                        }));
                    }
                }
            }

            if (query.ContainsKey("work_search[other_tag_names]"))
            {
                foreach (var tag in query["work_search[other_tag_names]"][0].Split(','))
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        return new KeyValuePair<Ao3TagType, string>(await LookupTagCategory(tag), UnescapeTag(tag));
                    }));
                }

            }


            if (query.ContainsKey("work_search[language_id]"))
            {
                int id;
                if (int.TryParse(query["work_search[language_id]"][0], out id))
                {
                    tasks.Add(Task.Run(async () => {
                        model.Language = await LookupLanguage(id);
                        return new KeyValuePair<Ao3TagType, string>(Ao3TagType.Other, null);
                    }));
                }
            }


            if (query.ContainsKey("work_search[complete]"))
            {
                int i;
                bool b;
                if (int.TryParse(query["work_search[complete]"][0], out i))
                    model.Complete = i != 0;
                else if (bool.TryParse(query["work_search[complete]"][0], out b))
                    model.Complete = b;
            }

            if (query.ContainsKey("work_search[query]"))
            {
                model.SearchQuery = query["work_search[query]"][0];
            }

            if (query.ContainsKey("tag_id"))
            {
                model.PrimaryTag = await LookupTag(query["tag_id"][0]);
            }

            Dictionary<Ao3TagType, List<string>> tags = model.Tags = new Dictionary<Ao3TagType, List<string>>();

            // Now deal with tags that we looked up
            foreach (var t in await Task.WhenAll(tasks))
            {
                if (string.IsNullOrEmpty(t.Value))
                    continue;

                List<string> list;
                if (!tags.TryGetValue(t.Key, out list))
                {
                    tags[t.Key] = list = new List<string>();
                }
                list.Add(t.Value);
            }
        }
    }
}
