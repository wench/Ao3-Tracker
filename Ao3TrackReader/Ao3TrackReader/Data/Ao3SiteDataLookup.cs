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

    public static class Ao3SiteDataLookup
    {
        static string[] escTagStrings = { "*s*", "*a*", "*d*", "*q*", "*h*" };
        static string[] usescTagStrings = { "/", "&", ".", "?", "#" };
        static Regex regexEscTag = new Regex(@"([/&.?#])");
        static Regex regexUnescTag = new Regex(@"(\*[sadqh])\*");
        static Regex regexTag = new Regex(@"^/tags/(?<TAGNAME>[^/?#]+)(/(?<TYPE>(works|bookmarks)?))?$", RegexOptions.ExplicitCapture);
        static Regex regexWork = new Regex(@"^/works/(?<WORKID>\d+)(/chapters/(?<CHAPTERID>\d+))?$", RegexOptions.ExplicitCapture);
        static Regex regexWorkComment = new Regex(@"^/works/(?<WORKID>\d+)/comments/(?<COMMENTID>\d+)$", RegexOptions.ExplicitCapture);
        static Regex regexRSSTagTitle = new Regex(@"AO3 works tagged '(?<TAGNAME>.*)'$", RegexOptions.ExplicitCapture);
        static Regex regexTagCategory = new Regex(@"This tag belongs to the (?<CATEGORY>\w*) Category\.", RegexOptions.ExplicitCapture);
        static Regex regexPageQuery = new Regex(@"(?<PAGE>&?page=\d+&?)");
        static HttpClient HttpClient { get; set; }

        static Ao3SiteDataLookup()
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.AllowAutoRedirect = false;
            HttpClient = new HttpClient(httpClientHandler);
        }

        static string EscapeTag(string tag)
        {
            if (tag == null) return null;
            return regexEscTag.Replace(tag, (match) =>
            {
                int i = Array.IndexOf(usescTagStrings, match.Value);
                if (i != -1) return escTagStrings[i];
                return "";
            });
        }

        static string UnescapeTag(string tag)
        {
            if (tag == null) return null;
            return regexUnescTag.Replace(tag, (match) =>
            {
                int i = Array.IndexOf(escTagStrings, match.Value);
                if (i != -1) return usescTagStrings[i];
                return "";
            });
        }

        static public string LookupTagQuick(int tagid)
        {
            string tag = App.Database.GetTag(tagid);
            if (!string.IsNullOrEmpty(tag))
                return tag;

            return null;
        }

        static public async Task<string> LookupTagAsync(int tagid)
        {
            string tag = App.Database.GetTag(tagid);
            if (!string.IsNullOrEmpty(tag))
                return tag;

            tag = null;
            var uri = new Uri(@"https://archiveofourown.org/tags/feed/" + tagid.ToString());

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get,uri);
            message.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/atom+xml"));
            try
            {
                var response = await HttpClient.SendAsync(message, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);

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

                        try
                        {
                            xml.Read();

                            var m = regexRSSTagTitle.Match(xml.ReadContentAsString());
                            if (m.Success)
                            {
                                tag = m.Groups["TAGNAME"].Value;
                            }
                        }
                        catch
                        {

                        }

                    }
                }

                if (tag != null) App.Database.SetTagId(tag, tagid);
            }
            catch (HttpRequestException)
            {

            }
            catch (TaskCanceledException)
            {

            }
            return tag;
        }

        static public TagCache LookupTagQuick(string intag)
        {
            intag = UnescapeTag(intag);
            var tag = App.Database.GetTag(intag) ?? new TagCache { name = intag };
            if (!string.IsNullOrEmpty(tag.actual))
            {
                return tag;
            }
            return null;
        }

        static public async Task<TagCache> LookupTagAsync(string intag)
        {
            intag = UnescapeTag(intag);
            var tag = App.Database.GetTag(intag) ?? new TagCache { name = intag };
            if (!string.IsNullOrEmpty(tag.actual))
            {
                return tag;
            }
            tag.actual = intag;
            intag = EscapeTag(intag);

            var uri = new Uri(@"https://archiveofourown.org/tags/" + intag);

            try
            {
                var response = await HttpClient.GetAsync(uri);

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
                                    var newuri = new Uri(uri, href.Value.HtmlDecode());
                                    var m = regexTag.Match(newuri.LocalPath);
                                    if (m.Success) tag.actual = UnescapeTag(m.Groups["TAGNAME"].Value);
                                    break;
                                }
                            }
                        }

                        // Parents
                        HtmlNode parenttagsnode = tagnode.ElementByClass("div", "parent")?.Element("ul");
                        if (parenttagsnode != null)
                        {
                            foreach (var e in parenttagsnode.DescendantsByClass("a", "tag"))
                            {
                                var href = e.Attributes["href"];
                                if (href != null && !string.IsNullOrEmpty(href.Value))
                                {
                                    var newuri = new Uri(uri, href.Value.HtmlDecode());
                                    var m = regexTag.Match(newuri.LocalPath);
                                    if (m.Success && !string.IsNullOrWhiteSpace(m.Groups["TAGNAME"].Value))
                                        tag.parents.Add(UnescapeTag(m.Groups["TAGNAME"].Value));
                                }
                            }

                        }

                        // Category
                        foreach (var p in tagnode.Elements("p"))
                        {
                            if (p.InnerText != null)
                            {
                                var m = regexTagCategory.Match(p.InnerText.HtmlDecode());
                                if (m.Success)
                                {
                                    tag.category = m.Groups["CATEGORY"].Value;
                                    break;
                                }
                            }
                        }
                    }

                }

                App.Database.SetTagDetails(tag);
            }
            catch (HttpRequestException)
            {

            }
            catch (TaskCanceledException)
            {

            }
            return tag;
        }

        static public Ao3TagType GetTypeForCategory(string category)
        {
            if (!string.IsNullOrEmpty(category))
            {
                Ao3TagType type;
                if (Enum.TryParse(category, true, out type) || Enum.TryParse(category + "s", true, out type))
                    return type;
            }

            return Ao3TagType.Other;
        }
        static public string LookupLanguageQuick(int langid)
        {
            string name = App.Database.GetLanguage(langid);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return null;
        }

        static public async Task<string> LookupLanguageAsync(int langid)
        {
            string name = App.Database.GetLanguage(langid);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            var uri = new Uri(@"https://archiveofourown.org/works/search");

            var response = await HttpClient.GetAsync(uri);

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
                            var n = opt.InnerText.HtmlDecode().Trim();
                            App.Database.SetLanguage(n, i);
                            if (i == langid) name = n;
                        }
                    }

                }

            }

            return name;
        }

        public static Uri CanonicalUri(string url)
        {
            var uribuilder = new UriBuilder(regexPageQuery.Replace(url, (m) => {
                if (m.Value.StartsWith("&") && m.Value.EndsWith("&")) return "&";
                else return "";
            }).TrimEnd('?'));

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
                return null;
            }

            uribuilder.Fragment = null;

            Uri uri = uribuilder.Uri;

            Match match = null;

            if ((match = regexWork.Match(uri.LocalPath)).Success)   // View Work
            {
                var sWORKID = match.Groups["WORKID"].Value;
                uri = new Uri(uri, "/works/" + sWORKID);
            }

            return uri;
        }
        public static IDictionary<string, Ao3PageModel> LookupQuick(ICollection<string> urls)
        {
            var dict = new Dictionary<string, Ao3PageModel>(urls.Count);

            foreach (string url in urls)
            {
                var uribuilder = new UriBuilder(regexPageQuery.Replace(url, (m) => {
                    if (m.Value.StartsWith("&") && m.Value.EndsWith("&")) return "&";
                    else return "";
                }).TrimEnd('?'));

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
                    dict[url] = null;
                    continue;
                }

                uribuilder.Fragment = null;

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

                    FillModelFromSearchQueryQuick(uri, model);
                }
                if (uri.LocalPath == "/works/search") // Work search and Advanced search
                {
                    model.Type = Ao3PageType.Search;
                    model.Title = "Advanced Search";

                    FillModelFromSearchQueryQuick(uri, model);

                }
                else if ((match = regexTag.Match(uri.LocalPath)).Success)    // View tag
                {
                    model.Type = Ao3PageType.Tag;

                    var sTAGNAME = match.Groups["TAGNAME"].Value;
                    var sTYPE = match.Groups["TYPE"].Value;

                    model.Title = (sTYPE ?? "").Trim();
                    model.Title = model.Title[0].ToString().ToUpper() + model.Title.Substring(1);

                    if (sTYPE == "works")
                    {
                        model.Type = Ao3PageType.Search;
                        if (uri.Query.Contains("work_search"))
                            model.Title = "Search";
                    }
                    else if (sTYPE == "bookmarks")
                        model.Type = Ao3PageType.Bookmarks;
                    else
                        model.Type = Ao3PageType.Tag;

                    var tagdetails = LookupTagQuick(sTAGNAME);
                    model.PrimaryTag = tagdetails?.actual ?? sTAGNAME;
                    model.PrimaryTagType = GetTypeForCategory(tagdetails?.category);
                    if (tagdetails != null)
                    {
                        SortedDictionary<Ao3TagType, List<string>> tags = model.Tags = new SortedDictionary<Ao3TagType, List<string>>();
                        foreach (string ptag in tagdetails.parents)
                        {
                            var ptagdetails = LookupTagQuick(ptag);

                            var actual = ptagdetails?.actual ?? ptag;
                            var tt = GetTypeForCategory(ptagdetails?.category);

                            List<string> list;
                            if (!tags.TryGetValue(tt, out list))
                            {
                                tags[tt] = list = new List<string>();
                            }
                            list.Add(actual);
                        }
                    }

                    FillModelFromSearchQueryQuick(uri, model);
                }
                else if ((match = regexWork.Match(uri.LocalPath)).Success)   // View Work
                {
                    model.Type = Ao3PageType.Work;
                    model.PrimaryTag = "<Work>";
                    model.PrimaryTagType = Ao3TagType.Other;

                    var sWORKID = match.Groups["WORKID"].Value;
                    model.Uri = uri = new Uri(uri, "/works/" + sWORKID);
                    model.Title = "Work " + sWORKID;

                    model.Details = new Ao3WorkDetails();

                    try
                    {
                        model.Details.WorkId = long.Parse(sWORKID);
                    }
                    catch
                    {

                    }

                }
                else
                {
                    model.Type = Ao3PageType.Unknown;
                    if (uri.LocalPath == "/") model.Title = "Archive of Our Own Home Page";
                    //model.Title = uri.ToString();
                }

                dict[url] = model;
            }

            return dict;
        }

        public static async Task<IDictionary<string, Ao3PageModel>> LookupAsync(ICollection<string> urls)
        {
            var tasks = new List<Task<KeyValuePair<string, Ao3PageModel>>>(urls.Count);
            
            foreach (string url in urls)
            { 
                tasks.Add(Task.Run(async () => {
                    var uribuilder = new UriBuilder(regexPageQuery.Replace(url, (m) => {
                        if (m.Value.StartsWith("&") && m.Value.EndsWith("&")) return "&";
                        else return "";
                    }).TrimEnd('?'));

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

                    uribuilder.Fragment = null;

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

                        await FillModelFromSearchQueryAsync(uri, model);
                    }
                    if (uri.LocalPath == "/works/search") // Work search and Advanced search
                    {
                        model.Type = Ao3PageType.Search;
                        model.Title = "Advanced Search";

                        await FillModelFromSearchQueryAsync(uri, model);

                    }
                    else if ((match = regexTag.Match(uri.LocalPath)).Success)    // View tag
                    {
                        model.Type = Ao3PageType.Tag;

                        var sTAGNAME = match.Groups["TAGNAME"].Value;
                        var sTYPE = match.Groups["TYPE"].Value;

                        model.Title = (sTYPE ?? "").Trim();
                        model.Title = model.Title[0].ToString().ToUpper() + model.Title.Substring(1);

                        if (sTYPE == "works")
                        {
                            model.Type = Ao3PageType.Search;
                            if (uri.Query.Contains("work_search"))
                                model.Title = "Search";
                        }
                        else if (sTYPE == "bookmarks")
                        {
                            model.Type = Ao3PageType.Bookmarks;
                        }
                        else
                        {
                            model.Type = Ao3PageType.Tag;
                        }

                        var tagdetails = await LookupTagAsync(sTAGNAME);
                        model.PrimaryTag = tagdetails.actual;
                        model.PrimaryTagType = GetTypeForCategory(tagdetails.category);

                        var tagtasks = new List<Task<KeyValuePair<Ao3TagType, string>>>(tagdetails.parents.Count);
                        foreach (string ptag in tagdetails.parents)
                        {
                            tagtasks.Add(Task.Run(async () =>
                            {
                                var ptagdetails = await LookupTagAsync(ptag);
                                return new KeyValuePair<Ao3TagType, string>(GetTypeForCategory(ptagdetails.category), ptag);
                            }));
                        }
                        SortedDictionary<Ao3TagType, List<string>> tags = model.Tags = new SortedDictionary<Ao3TagType, List<string>>();
                        foreach (var t in await Task.WhenAll(tagtasks))
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

                        await FillModelFromSearchQueryAsync(uri, model);

                    }
                    else if ((match = regexWork.Match(uri.LocalPath)).Success)   // View Work
                    {
                        model.Type = Ao3PageType.Work;

                        var sWORKID = match.Groups["WORKID"].Value;
                        model.Uri = uri = new Uri(uri,"/works/" + sWORKID);

                        model.Details = new Ao3WorkDetails();

                        try
                        {
                            model.Details.WorkId = long.Parse(sWORKID);
                        }
                        catch
                        {

                        }

                        var wsuri = new Uri(@"https://archiveofourown.org/works/search?utf8=%E2%9C%93&work_search%5Bquery%5D=id%3A" + sWORKID);
                        try
                        {
                            var response = await HttpClient.GetAsync(wsuri);

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

                                var worknode = doc.GetElementbyId("work_" + sWORKID);

                                if (worknode == null)
                                {
                                    // No worknode, try with cookies
                                    string cookies = App.Database.GetVariable("siteCookies");
                                    if (!string.IsNullOrWhiteSpace(cookies))
                                    {
                                        var request = new HttpRequestMessage(HttpMethod.Get, wsuri);
                                        request.Headers.Add("Cookie", cookies);
                                        response = await HttpClient.SendAsync(request);

                                        if (response.IsSuccessStatusCode)
                                        {
                                            try
                                            {
                                                encoding = Encoding.GetEncoding(response.Content.Headers.ContentType.CharSet);
                                            }
                                            catch
                                            {
                                            }
                                            doc.Load(await response.Content.ReadAsStreamAsync(), encoding);
                                            worknode = doc.GetElementbyId("work_" + sWORKID);
                                        }
                                    }
                                }

                                if (worknode != null) await FillModelFromWorkSummaryAsync(wsuri, worknode, model);
                            }
                        }
                        catch (HttpRequestException)
                        {

                        }
                        catch (TaskCanceledException)
                        {

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

        private static async Task FillModelFromWorkSummaryAsync(Uri baseuri, HtmlNode worknode, Ao3PageModel model)
        {
            // Gather all tags
            SortedDictionary<Ao3TagType, List<string>> tags = model.Tags = new SortedDictionary<Ao3TagType, List<string>>();

            // Tags node
            HtmlNode tagsnode = worknode?.ElementByClass("ul", "tags");

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
                        var reluri = new Uri(baseuri, href.Value.HtmlDecode());
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
            var headernode = worknode?.ElementByClass("div", "header");

            // Get Fandom tags
            HtmlNode fandomnode = headernode?.ElementByClass("fandoms");
            if (fandomnode != null)
            {
                foreach (var a in fandomnode.Elements("a"))
                {
                    var href = a.Attributes["href"];
                    if (href != null && !string.IsNullOrEmpty(href.Value))
                    {
                        var reluri = new Uri(baseuri, href.Value.HtmlDecode());
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
                    model.Title = titlenode?.InnerText?.HtmlDecode();

                    foreach (var n in links)
                    {
                        var href = n.Attributes["href"];
                        var rel = n.Attributes["rel"];
                        var uri = new Uri(baseuri, href.Value.HtmlDecode());

                        if (rel?.Value == "author")
                        {
                            authors[uri.AbsoluteUri] = n.InnerText.HtmlDecode();
                            continue;
                        }

                        if (href.Value.EndsWith("/gifts"))
                        {
                            recipiants[uri.AbsoluteUri] = n.InnerText.HtmlDecode();
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
                var search = n.Key.ToString();
                var tag = Array.Find(classes, (val) =>
                {
                    return val.StartsWith(search + "-", StringComparison.OrdinalIgnoreCase) || val.StartsWith(search.TrimEnd('s') + "-", StringComparison.OrdinalIgnoreCase);
                });

                if (tag == null)
                    model.RequiredTags[n.Key] = null;
                else
                    model.RequiredTags[n.Key] = new Tuple<string, string>(tag, n.Value.InnerText.HtmlDecode().Trim());

            }

            model.PrimaryTag = null;

            // Get primary tag... 
            if (tags.ContainsKey(Ao3TagType.Relationships) && tags[Ao3TagType.Relationships].Count > 0)
            {
                var tagdetails = await LookupTagAsync(tags[Ao3TagType.Relationships][0]);
                model.PrimaryTag = tagdetails.actual;
                model.PrimaryTagType = Ao3TagType.Relationships;
            }
            else if (tags.ContainsKey(Ao3TagType.Fandoms) && tags[Ao3TagType.Fandoms].Count > 0)
            {
                var tagdetails = await LookupTagAsync(tags[Ao3TagType.Fandoms][0]);
                model.PrimaryTag = tagdetails.actual;
                model.PrimaryTagType = Ao3TagType.Fandoms;
            }

            if (model.PrimaryTag != null)
            {
                var tagdetails = await LookupTagAsync(model.PrimaryTag);
                model.PrimaryTag = tagdetails.actual;
                model.PrimaryTagType = GetTypeForCategory(tagdetails.category);
            }

            // Stats

            var stats = worknode?.ElementByClass("dl", "stats");

            model.Details.LastUpdated = headernode?.ElementByClass("p", "datetime")?.InnerText?.HtmlDecode()?.Trim();

            model.Language = stats?.ElementByClass("dd", "language")?.InnerText?.HtmlDecode()?.Trim();

            try
            {
                if (stats != null) model.Details.Words = int.Parse(stats.ElementByClass("dd", "words")?.InnerText?.HtmlDecode()?.Replace(",", ""));
            }
            catch
            {

            }

            try
            {
                if (stats != null) model.Details.Collections = int.Parse(stats.ElementByClass("dd", "collections")?.InnerText?.HtmlDecode());
            }
            catch
            {

            }

            try
            {
                if (stats != null) model.Details.Comments = int.Parse(stats.ElementByClass("dd", "comments")?.InnerText?.HtmlDecode());
            }
            catch
            {

            }

            try
            {
                if (stats != null) model.Details.Kudos = int.Parse(stats.ElementByClass("dd", "kudos")?.InnerText?.HtmlDecode());
            }
            catch
            {

            }

            try
            {
                if (stats != null) model.Details.Bookmarks = int.Parse(stats.ElementByClass("dd", "bookmarks")?.InnerText?.HtmlDecode());
            }
            catch
            {

            }

            try
            {
                if (stats != null) model.Details.Hits = int.Parse(stats.ElementByClass("dd", "hits")?.InnerText?.HtmlDecode());
            }
            catch
            {

            }

            // Series

            var seriesnode = worknode?.ElementByClass("ul", "series");
            if (seriesnode != null)
            {
                Dictionary<string, Tuple<int, string>> series = new Dictionary<string, Tuple<int, string>>(1);
                foreach (var n in seriesnode.Elements("li"))
                {
                    var link = n.Element("a");
                    if (link == null || String.IsNullOrWhiteSpace(link.InnerText)) continue;

                    var s = link.Attributes["href"]?.Value;
                    if (String.IsNullOrWhiteSpace(s)) continue;
                    var uri = new Uri(baseuri, s.HtmlDecode());

                    var part = n.Element("strong")?.InnerText?.HtmlDecode();
                    if (String.IsNullOrWhiteSpace(part)) continue;

                    try
                    {
                        series[uri.AbsoluteUri] = new Tuple<int, string>(int.Parse(part), link.InnerText?.HtmlDecode());
                    }
                    catch
                    {

                    }

                }
                if (series.Count > 0) model.Details.Series = series;
            }

            var chapters = stats?.ElementByClass("dd", "chapters")?.InnerText?.Trim()?.Split('/');
            if (chapters != null)
            {
                try
                {
                    int? total;
                    if (chapters[1] == "?") total = null;
                    else total = int.Parse(chapters[1]);
                    var tworkchaps = App.Storage.getWorkChaptersAsync(new[] { model.Details.WorkId });
                    Helper.WorkChapter workchap = null;
                    tworkchaps.Result.TryGetValue(model.Details.WorkId, out workchap);
                    int chapters_finished = workchap != null ? (int)workchap.number : (int)0;
                    if (workchap?.location != null) { chapters_finished--; }

                    model.Details.Chapters = new Tuple<int?, int, int?>(chapters_finished, int.Parse(chapters[0]), total);
                }
                catch
                {

                }
            }

            // Horrible horrible dirty grabbing of the summary
            var summarynode = worknode?.ElementByClass("blockquote", "summary");
            if (summarynode != null)
            {

                try
                {
                    model.Details.Summary = HtmlConverter.ConvertNode(summarynode);
                }
                catch
                {

                }
            }
        }

        private static async Task FillModelFromSearchQueryAsync(Uri uri, Ao3PageModel model)
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
                            string tag = await LookupTagAsync(id);
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
                        var tagdetails = await LookupTagAsync(tag);
                        return new KeyValuePair<Ao3TagType, Tuple<string, int>>(GetTypeForCategory(tagdetails.category), new Tuple<string,int>(UnescapeTag(tag),0));
                    }));
                }

            }


            if (query.ContainsKey("work_search[language_id]"))
            {
                int id;
                if (int.TryParse(query["work_search[language_id]"][0], out id))
                {
                    tasks.Add(Task.Run(async () => {
                        model.Language = await LookupLanguageAsync(id);
                        return new KeyValuePair<Ao3TagType, Tuple<string, int>>(Ao3TagType.Other, null);
                    }));
                }
                else if (query["work_search[language_id]"][0] == "")
                {
                    model.Language = "Any";
                }
            }
            else
            {
                model.Language = "Any";
            }


            if (query.ContainsKey("work_search[complete]"))
            {
                int i = 0;
                bool b = false;
                if (int.TryParse(query["work_search[complete]"][0], out i) || bool.TryParse(query["work_search[complete]"][0], out b)) {
                    if (i != 0 || b)
                    {
                        model.RequiredTags[Ao3RequiredTag.Complete] = new Tuple<string, string>("complete-yes", "Complete only");
                    }
                    else
                    {
                    }
                }
            }
            if (!model.RequiredTags.ContainsKey(Ao3RequiredTag.Complete) || model.RequiredTags[Ao3RequiredTag.Complete] == null)
                model.RequiredTags[Ao3RequiredTag.Complete] = new Tuple<string, string>("category-none", "Complete and Incomplete");

            tasks.Add(Task.Run(() => {
                return new KeyValuePair<Ao3TagType, Tuple<string, int>>(Ao3TagType.Other, new Tuple<string, int>(model.RequiredTags[Ao3RequiredTag.Complete].Item2, 0));
            }));

            if (query.ContainsKey("work_search[query]"))
            {
                model.SearchQuery = query["work_search[query]"][0];
            }

            if (query.ContainsKey("tag_id"))
            {
                var tagdetails = await LookupTagAsync(query["tag_id"][0]);
                model.PrimaryTag = tagdetails.actual;
                model.PrimaryTagType = GetTypeForCategory(tagdetails.category);
            }

            // Now deal with tags that we looked up            
            SortedDictionary<Ao3TagType, List<string>> tags = model.Tags = model.Tags ?? new SortedDictionary<Ao3TagType, List<string>>();
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
                if (!list.Contains(t.Value.Item1))
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

        private static void FillModelFromSearchQueryQuick(Uri uri, Ao3PageModel model)
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
            var tlist = new List<KeyValuePair<Ao3TagType, Tuple<string, int>>>();

            foreach (var i in Enum.GetValues(typeof(Ao3TagType)))
            {
                List<string> tagids;
                var name = "work_search[" + i.ToString().ToLowerInvariant().TrimEnd('s') + "_ids][]";
                if (query.TryGetValue(name, out tagids))
                {
                    foreach (var s in tagids)
                    {
                        int id;
                        if (!int.TryParse(s, out id)) continue;

                        string tag = LookupTagQuick(id);
                        if (!string.IsNullOrWhiteSpace(tag))
                            tlist.Add(new KeyValuePair<Ao3TagType, Tuple<string, int>>((Ao3TagType)i, new Tuple<string, int>(tag, id)));
                    }
                }
            }

            if (query.ContainsKey("work_search[other_tag_names]"))
            {
                foreach (var tag in query["work_search[other_tag_names]"][0].Split(','))
                {
                    var tagdetails = LookupTagQuick(tag);
                    tlist.Add(new KeyValuePair<Ao3TagType, Tuple<string, int>>(GetTypeForCategory(tagdetails?.category), new Tuple<string, int>(UnescapeTag(tag), 0)));
                }

            }


            if (query.ContainsKey("work_search[language_id]"))
            {
                int id;
                if (int.TryParse(query["work_search[language_id]"][0], out id))
                {
                    model.Language = LookupLanguageQuick(id);
                }
                else if (query["work_search[language_id]"][0] == "")
                {
                    model.Language = "Any";
                }
            }
            else
            {
                model.Language = "Any";
            }


            if (query.ContainsKey("work_search[complete]"))
            {
                int i = 0;
                bool b = false;
                if (int.TryParse(query["work_search[complete]"][0], out i) || bool.TryParse(query["work_search[complete]"][0], out b))
                {
                    if (i != 0 || b)
                    {
                        model.RequiredTags[Ao3RequiredTag.Complete] = new Tuple<string, string>("complete-yes", "Complete only");
                    }
                    else
                    {
                    }
                }
            }
            if (!model.RequiredTags.ContainsKey(Ao3RequiredTag.Complete) || model.RequiredTags[Ao3RequiredTag.Complete] == null)
                model.RequiredTags[Ao3RequiredTag.Complete] = new Tuple<string, string>("category-none", "Complete and Incomplete");

            tlist.Add(new KeyValuePair<Ao3TagType, Tuple<string, int>>(Ao3TagType.Other, new Tuple<string, int>(model.RequiredTags[Ao3RequiredTag.Complete].Item2, 0)));

            if (query.ContainsKey("work_search[query]"))
            {
                model.SearchQuery = query["work_search[query]"][0];
            }

            if (query.ContainsKey("tag_id"))
            {
                var tagdetails = LookupTagQuick(query["tag_id"][0]);
                model.PrimaryTag = tagdetails?.actual ?? query["tag_id"][0];
                model.PrimaryTagType = GetTypeForCategory(tagdetails?.category);
            }

            // Now deal with tags that we looked up            
            SortedDictionary<Ao3TagType, List<string>> tags = model.Tags = model.Tags ?? new SortedDictionary<Ao3TagType, List<string>>();
            Dictionary<string, int> idmap = new Dictionary<string, int>(tlist.Count);
            foreach (var t in tlist)
            {
                if (t.Value == null || string.IsNullOrEmpty(t.Value.Item1))
                    continue;

                if (t.Value.Item2 != 0) idmap[t.Value.Item1] = t.Value.Item2;

                List<string> list;
                if (!tags.TryGetValue(t.Key, out list))
                {
                    tags[t.Key] = list = new List<string>();
                }
                if (!list.Contains(t.Value.Item1))
                    list.Add(t.Value.Item1);
            }

            // Generate required tags
            List<string> req;
            if (tags.TryGetValue(Ao3TagType.Warnings, out req))
            {
                int id = 0;
                string sclass;
                if (req.Count == 1 && idmap.TryGetValue(req[0], out id) && TagIdToReqClass.TryGetValue(id, out sclass))
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
