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
        static string[] escTagStrings = { "*s*", "*a*", "*d*", "*q*", "*h*" };
        static string[] usescTagStrings = { "/", "&", ".", "?", "#" };
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
                int i = Array.IndexOf(usescTagStrings, match.Value);
                if (i != -1) return escTagStrings[i];
                return "";
            });
        }

        static string UnescapeTag(string tag)
        {
            return regexUnescTag.Replace(tag, (match) =>
            {
                int i = Array.IndexOf(escTagStrings, match.Value);
                if (i != -1) return usescTagStrings[i];
                return "";
            });
        }

        static async Task<string> LookupTag(int tagid)
        {
            string tag = App.Database.GetTagForId(tagid);
            if (!string.IsNullOrEmpty(tag))
                return tag;

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

            if (tag != null) App.Database.SetTagId(tag, tagid);
            return tag;
        }

        static async Task<string> LookupTag(string intag)
        {
            intag = UnescapeTag(intag);
            string tag = App.Database.GetTagMerger(intag);
            if (!string.IsNullOrEmpty(tag))
            {
                return tag;
            }
            tag = intag;
            intag = EscapeTag(intag);

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
                                if (m.Success) tag = UnescapeTag(m.Groups["TAGNAME"].Value);
                                break;
                            }
                        }
                    }

                    // Category
                    foreach (var p in tagnode.Elements("p"))
                    {
                        if (p.InnerText != null)
                        {
                            var m = regexTagCategory.Match(p.InnerText);
                            if (m.Success)
                            {
                                App.Database.SetTagCategory(intag, m.Groups["CATEGORY"].Value);
                                if (tag != intag) App.Database.SetTagCategory(tag, m.Groups["CATEGORY"].Value);
                                break;
                            }
                        }
                    }
                }

            }

            App.Database.SetTagMerger(intag, UnescapeTag(tag));
            return tag;
        }

        static async Task<Ao3TagType> LookupTagCategory(string tag)
        {
            tag = UnescapeTag(tag);
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
                    var uribuilder = new UriBuilder(url);

                    if (uribuilder.Host == "archiveofourown.org" || uribuilder.Host == "www.archiveofourown.org")
                    {
                        if (uribuilder.Scheme == "http")
                        {
                            uribuilder.Scheme = "https";
                        }
                        uribuilder.Port = -1;
                    }
                    else
                    {
                        return new KeyValuePair<string, Ao3PageModel>(url, null);
                    }

                    Uri uri = uribuilder.Uri;

                    Ao3PageModel model = new Ao3PageModel
                    {
                        Uri = uri
                    };

                    Match match = null;

                    if (uri.LocalPath == "/works") // Work search and Advanced search
                    {
                        model.Type = Ao3PageType.Search;
                        model.Title = "Search";

                        await FillModelFromSearchQuery(uri, model);
                    }
                    if (uri.LocalPath == "/works/search") // Work search and Advanced search
                    {
                        model.Type = Ao3PageType.Search;
                        model.Title = "Advanced Search";

                        await FillModelFromSearchQuery(uri, model);

                    }
                    else if ((match = regexTag.Match(uri.LocalPath)).Success)    // View tag
                    {
                        model.Type = Ao3PageType.Tag;

                        var sTAGNAME = match.Groups["TAGNAME"].Value;
                        var sTYPE = match.Groups["TYPE"].Value;

                        if (sTYPE == "works")
                        {
                            model.Type = Ao3PageType.Search;
                            model.Title = "Works";
                        }
                        else if (sTYPE == "bookmarks")
                        {
                            model.Type = Ao3PageType.Bookmarks;
                            model.Title = "Bookmarks";
                        }
                        else
                        {
                            model.Type = Ao3PageType.Tag;
                            model.Title = "Tag";

                        }

                        model.PrimaryTag = await LookupTag(sTAGNAME);
                        model.PrimaryTagType = await LookupTagCategory(model.PrimaryTag);
                    }
                    else if ((match = regexWork.Match(uri.LocalPath)).Success)   // View Work
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
                        if (uri.LocalPath == "/") model.Title = "Archive of Our Own Home Page";
                        //model.Title = uri.ToString();
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
            model.Details = new Ao3WorkDetails();

            // Gather all tags
            SortedDictionary<Ao3TagType, List<string>> tags = model.Tags = new SortedDictionary<Ao3TagType, List<string>>();

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

            HtmlNode headingnode = headernode?.ElementByClass("heading");
            if (headingnode != null)
            {
                var links = headingnode.Elements("a");
                Dictionary<string, string> authors = new Dictionary<string, string>(1);
                Dictionary<string, string> recipiants = new Dictionary<string, string>();
                if (links != null)
                {
                    var titlenode = links.FirstOrDefault();
                    model.Title = titlenode?.InnerText;

                    foreach (var n in links)
                    {
                        var href = n.Attributes["href"];
                        var rel = n.Attributes["rel"];
                        var uri = new Uri(baseuri, href.Value);

                        if (rel?.Value == "author")
                        {
                            authors[uri.AbsoluteUri] = n.InnerText;
                            continue;
                        }

                        if (href.Value.EndsWith("/gifts"))
                        {
                            recipiants[uri.AbsoluteUri] = n.InnerText;
                        }
                    }
                }
                if (authors.Count != 0) model.Details.Authors = authors;
                if (recipiants.Count != 0) model.Details.Recipiants = recipiants;
            }

            // Get requried tags
            HtmlNode requirednode = headernode?.ElementByClass("required-tags");
            Dictionary<Ao3RequiredTag, HtmlNode> required = new Dictionary<Ao3RequiredTag, HtmlNode>(4);
            required[Ao3RequiredTag.Rating] = requirednode?.DescendantsByClass("span", "rating")?.FirstOrDefault();
            required[Ao3RequiredTag.Warnings] = requirednode?.DescendantsByClass("span", "warnings")?.FirstOrDefault();
            required[Ao3RequiredTag.Category] = requirednode?.DescendantsByClass("span", "category")?.FirstOrDefault();
            required[Ao3RequiredTag.Complete] = requirednode?.DescendantsByClass("span", "iswip")?.FirstOrDefault();

            model.RequiredTags = new Dictionary<Ao3RequiredTag, Tuple<string, string>>(4);
            foreach (var n in required)
            {
                if (n.Value == null)
                {
                    model.RequiredTags[n.Key] = null;
                    continue;
                }

                var classes = n.Value.GetClasses();
                var search = n.Key.ToString() + "-";
                var tag = Array.Find(classes, (val) =>
                {
                    return val.StartsWith(search, StringComparison.OrdinalIgnoreCase);
                });

                if (tag == null)
                    model.RequiredTags[n.Key] = null;
                else
                    model.RequiredTags[n.Key] = new Tuple<string, string>(tag, n.Value.InnerText.Trim());

            }

            model.PrimaryTag = null;

            // Get primary tag... 
            if (tags.ContainsKey(Ao3TagType.Relationships) && tags[Ao3TagType.Relationships].Count > 0)
            {
                model.PrimaryTag = await LookupTag(tags[Ao3TagType.Relationships][0]);
                model.PrimaryTagType = Ao3TagType.Relationships;
            }
            else if (tags.ContainsKey(Ao3TagType.Fandoms) && tags[Ao3TagType.Fandoms].Count > 0)
            {
                model.PrimaryTag = await LookupTag(tags[Ao3TagType.Fandoms][0]);
                model.PrimaryTagType = Ao3TagType.Fandoms;
            }

            if (model.PrimaryTag != null)
            {
                model.PrimaryTag = await LookupTag(model.PrimaryTag);
                if (model.PrimaryTagType == Ao3TagType.Other) model.PrimaryTagType = await LookupTagCategory(model.PrimaryTag);
            }

            // Stats

            var stats = worknode.ElementByClass("dl", "stats");

            model.Details.LastUpdated = headernode?.ElementByClass("p", "datetime")?.InnerText?.Trim();

            model.Language = stats?.ElementByClass("dd", "language")?.InnerText?.Trim();

            try
            {
                model.Details.Words = int.Parse(stats?.ElementByClass("dd", "words")?.InnerText?.Replace(",", ""));
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
                    model.Details.Chapters = new Tuple<int, int?>(int.Parse(chapters[0]), total);
                }
                catch
                {

                }
            }

            try
            {
                model.Details.Collections = int.Parse(stats?.ElementByClass("dd", "collections")?.InnerText);
            }
            catch
            {

            }

            try
            {
                model.Details.Comments = int.Parse(stats?.ElementByClass("dd", "comments")?.InnerText);
            }
            catch
            {

            }

            try
            {
                model.Details.Kudos = int.Parse(stats?.ElementByClass("dd", "kudos")?.InnerText);
            }
            catch
            {

            }

            try
            {
                model.Details.Bookmarks = int.Parse(stats?.ElementByClass("dd", "bookmarks")?.InnerText);
            }
            catch
            {

            }

            try
            {
                model.Details.Hits = int.Parse(stats?.ElementByClass("dd", "hits")?.InnerText);
            }
            catch
            {

            }

            // Series

            var seriesnode = worknode.ElementByClass("ul", "series");
            if (seriesnode != null)
            {
                Dictionary<string, Tuple<int, string>> series = new Dictionary<string, Tuple<int, string>>(1);
                foreach (var n in seriesnode.Elements("li"))
                {
                    var link = n.Element("a");
                    if (link == null || String.IsNullOrWhiteSpace(link.InnerText)) continue;

                    var s = link.Attributes["href"]?.Value;
                    if (String.IsNullOrWhiteSpace(s)) continue;
                    var uri = new Uri(baseuri, s);

                    var part = n.Element("strong")?.InnerText;
                    if (String.IsNullOrWhiteSpace(part)) continue;

                    try
                    {
                        series[uri.AbsoluteUri] = new Tuple<int, string>(int.Parse(part), link.InnerText);
                    }
                    catch
                    {

                    }

                }
                if (series.Count > 0) model.Details.Series = series;
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


            model.RequiredTags = new Dictionary<Ao3RequiredTag, Tuple<string, string>>(4);
            var tasks = new List<Task<KeyValuePair<Ao3TagType, Tuple<string,int>>>>();

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
                            return new KeyValuePair<Ao3TagType, Tuple<string, int>>((Ao3TagType)i, new Tuple<string,int>(tag,id));
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
                        return new KeyValuePair<Ao3TagType, Tuple<string, int>>(await LookupTagCategory(tag), new Tuple<string,int>(UnescapeTag(tag),0));
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
                        return new KeyValuePair<Ao3TagType, Tuple<string, int>>(Ao3TagType.Other, null);
                    }));
                }
            }


            if (query.ContainsKey("work_search[complete]"))
            {
                int i = 0;
                bool b = false;
                if (int.TryParse(query["work_search[complete]"][0], out i) || bool.TryParse(query["work_search[complete]"][0], out b)) {
                    if (i != 0 || b)
                        model.RequiredTags[Ao3RequiredTag.Complete] = new Tuple<string, string>("complete-yes", "Complete only");
                    else
                        model.RequiredTags[Ao3RequiredTag.Complete] = new Tuple<string, string>("complete-no", "Complete and Incomplete");
                }
            }

            if (query.ContainsKey("work_search[query]"))
            {
                model.SearchQuery = query["work_search[query]"][0];
            }

            if (query.ContainsKey("tag_id"))
            {
                model.PrimaryTag = await LookupTag(query["tag_id"][0]);
                model.PrimaryTagType = await LookupTagCategory(model.PrimaryTag);
            }

            // Now deal with tags that we looked up
            SortedDictionary<Ao3TagType, List<string>> tags = model.Tags = new SortedDictionary<Ao3TagType, List<string>>();
            Dictionary<string,int> idmap = new Dictionary<string,int>(tasks.Count);
            foreach (var t in await Task.WhenAll(tasks))
            {
                if (t.Value == null || string.IsNullOrEmpty(t.Value.Item1))
                    continue;

                if (t.Value.Item2 != 0) idmap[t.Value.Item1] = t.Value.Item2;

                List<string> list;
                if (!tags.TryGetValue(t.Key, out list))
                {
                    tags[t.Key] = list = new List<string>();
                }
                list.Add(t.Value.Item1);
            }

            // Generate required tags
            List<string> req;
            if (tags.TryGetValue(Ao3TagType.Warnings, out req))
            {
                int id = 0;
                string sclass;
                if (req.Count == 1 && idmap.TryGetValue(req[0],out id) && TagIdToReqClass.TryGetValue(id,out sclass))
                    model.RequiredTags[Ao3RequiredTag.Warnings] = new Tuple<string, string>(sclass, req[0]);
                else
                    model.RequiredTags[Ao3RequiredTag.Warnings] = new Tuple<string, string>("warning-yes", string.Join(", ", req));
            }
            if (tags.TryGetValue(Ao3TagType.Category, out req))
            {
                int id = 0;
                string sclass;
                if (req.Count == 1 && idmap.TryGetValue(req[0], out id) && TagIdToReqClass.TryGetValue(id, out sclass))
                    model.RequiredTags[Ao3RequiredTag.Category] = new Tuple<string, string>(sclass, req[0]);
                else
                    model.RequiredTags[Ao3RequiredTag.Category] = new Tuple<string, string>("category-multi", string.Join(", ", req));
            }
            if (tags.TryGetValue(Ao3TagType.Rating, out req))
            {
                int id = 0;
                string sclass;
                if (req.Count == 1 && idmap.TryGetValue(req[0], out id) && TagIdToReqClass.TryGetValue(id, out sclass))
                    model.RequiredTags[Ao3RequiredTag.Rating] = new Tuple<string, string>(sclass, req[0]);
                else
                    model.RequiredTags[Ao3RequiredTag.Rating] = new Tuple<string, string>("rating-na", string.Join(", ", req));
            }
        }

        static Dictionary<int, string> TagIdToReqClass = new Dictionary<int, string> {
            { 14, "warning-choosenotto" },
            { 16, "warning-no" },
            { 10, "rating-general-audience"},
            { 11, "rating-teen"},
            { 12, "rating-mature"},
            { 13, "rating-explicit"},
            { 9, "rating-notrated"},
            { 116, "category-femslash"},
            { 22, "category-het"},
            { 21, "category-gen"},
            { 23, "category-slash"},
            { 2246, "category-multi"},
            { 24, "category-other"},
        };

    }
}
