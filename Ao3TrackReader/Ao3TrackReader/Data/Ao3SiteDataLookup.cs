using System;
using System.Collections.Generic;
using System.Text;
using Ao3TrackReader.Models;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Xml;
using System.Linq;

namespace Ao3TrackReader.Data
{
    // Data Grabbing from Ao3 itself or from URLs
    //
    // * For work: Get details from summary on a search page https://archiveofourown.org/works/search?utf8=%E2%9C%93&work_search%5Bquery%5D=id%3A(<<WORKIDS>>) where <<WORKIDS>> = number | number+OR+<<WORKIDS>>
    // * A tag page: https://archiveofourown.org/tags/<<TAGNAME>>[''|'/works'|'/bookmarks']
    // * in searches: https://archiveofourown.org/works?tag_id=<<TAGNAME>> and details from the rest of the crazy long query string
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
        public static HtmlNode ElementByClass(this HtmlNode node, string classname)
        {
            foreach (var e in node.ChildNodes)
            {
                if (e.NodeType != HtmlNodeType.Element) continue;

                var attrClass = e.Attributes["class"];
                if (attrClass != null && !string.IsNullOrEmpty(attrClass.Value))
                {
                    if (Array.IndexOf(attrClass.Value.Split(' '), classname) != -1)
                    {
                        return e;
                    }
                }
            }
            return null;
        }

        public static HtmlNode ElementByClass(this HtmlNode node, string tagname, string classname)
        {
            foreach (var e in node.Elements(tagname))
            {
                var attrClass = e.Attributes["class"];
                if (attrClass != null && !string.IsNullOrEmpty(attrClass.Value))
                {
                    if (Array.IndexOf(attrClass.Value.Split(' '), classname) != -1)
                    {
                        return e;
                    }
                }
            }
            return null;
        }

        public static IEnumerable<HtmlNode> ElementsByClass(this HtmlNode node, string classname)
        {
            foreach (var e in node.ChildNodes)
            {
                if (e.NodeType != HtmlNodeType.Element) continue;

                var attrClass = e.Attributes["class"];
                if (attrClass != null && !string.IsNullOrEmpty(attrClass.Value))
                {
                    if (Array.IndexOf(attrClass.Value.Split(' '), classname) != -1)
                    {
                        yield return e;
                    }
                }
            }
        }

        public static IEnumerable<HtmlNode> ElementsByClass(this HtmlNode node, string tagname, string classname)
        {
            foreach (var e in node.Elements(tagname))
            {
                var attrClass = e.Attributes["class"];
                if (attrClass != null && !string.IsNullOrEmpty(attrClass.Value))
                {
                    if (Array.IndexOf(attrClass.Value.Split(' '), classname) != -1)
                    {
                        yield return e;
                    }
                }
            }
        }

        public static IEnumerable<HtmlNode> DescendantsByClass(this HtmlNode node, string tagname, string classname)
        {
            foreach (var e in node.Descendants(tagname))
            {
                var attrClass = e.Attributes["class"];
                if (attrClass != null && !string.IsNullOrEmpty(attrClass.Value))
                {
                    if (Array.IndexOf(attrClass.Value.Split(' '), classname) != -1)
                    {
                        yield return e;
                    }
                }
            }
        }
    }


    public class Ao3SiteDataLookup
    {
        static Regex regexEscTag = new Regex(@"([/&.?#])");
        static Regex regexUnescTag = new Regex(@"(\*[sadqh])\*");
        static Regex regexTag = new Regex(@"^/tags/(?<TAGNAME>[^/&.?#,^<>{}`\\=]+)(/(?<TYPE>(works|bookmarks)?))?$", RegexOptions.ExplicitCapture);
        static Regex regexWork = new Regex(@"^/works/(?<WORKID>\d+)(/chapter/(?<CHAPTERID>\d+))?$", RegexOptions.ExplicitCapture);

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

        static async Task<string> LookupTag(string tag)
        {
            var uri = new Uri(@"https://archiveofourown.org/tags/" + tag);

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

                HtmlNode mergernode = main.ElementByClass("div", "tag")?.ElementByClass("div", "merger");
                if (mergernode == null) return tag;

                foreach (var e in mergernode.DescendantsByClass("a", "tag"))
                {
                    var href = e.Attributes["href"];
                    if (href != null && !string.IsNullOrEmpty(href.Value)) {
                        var newuri = new Uri(uri, href.Value);
                        var m = regexTag.Match(newuri.LocalPath);
                        if (m.Success) tag = m.Groups["TAGNAME"].Value;
                        break;
                    }
                }

            }

            return tag;
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

                    Match match;

                    if (uri.Path == "/works") // Work search
                    {

                    }
                    else if (uri.Path == "/works/search") // Work search
                    {

                    }
                    else if ((match = regexTag.Match(uri.Path)).Success)    // View tag
                    {
                        var sTAGNAME = match.Groups["TAGNAME"].Value;
                        var sTYPE = match.Groups["TYPE"].Value;

                        var tag = await LookupTag(sTAGNAME);

                    }
                    else if ((match = regexWork.Match(uri.Path)).Success)   // View Work
                    {
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

                            string tag = null;
                            // Grab first relationship tag
                            HtmlNode relnode = worknode.ElementByClass("ul", "tags")?.ElementByClass("li", "relationships");

                            if(relnode != null && (relnode = relnode.Element("a")) != null)
                            {
                                var reluri = new Uri(wsuri, relnode.Attributes["href"].Value);
                                var m = regexTag.Match(reluri.LocalPath);
                                if (m.Success) tag = await LookupTag(m.Groups["TAGNAME"].Value);
                            }

                            if (tag != null)
                                tag = UnescapeTag(tag);
                        }
                    }
                    return new KeyValuePair<string, Ao3PageModel>(url, null);
                }));
            }

            var dict = new Dictionary<string, Ao3PageModel>(urls.Count);
            foreach (var kp in await Task.WhenAll(tasks))
            {
                dict[kp.Key] = kp.Value;
            }
            return dict;
        }
    }
}
