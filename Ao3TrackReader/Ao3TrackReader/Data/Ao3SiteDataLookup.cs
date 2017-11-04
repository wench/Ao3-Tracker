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
using Ao3TrackReader.Models;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Xml;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;

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
        static public Regex regexTag = new Regex(@"^/tags/(?<TAGNAME>[^/?#]+)(/(?<TYPE>(works|bookmarks)?))?$", RegexOptions.ExplicitCapture);
        static public Regex regexWork = new Regex(@"^/works/(?<WORKID>\d+)(/chapters/(?<CHAPTERID>\d+))?$", RegexOptions.ExplicitCapture);
        static public Regex regexWorkComment = new Regex(@"^/works/(?<WORKID>\d+)/comments/(?<COMMENTID>\d+)$", RegexOptions.ExplicitCapture);
        static public Regex regexSeries = new Regex(@"^/series/(?<SERIESID>\d+)$", RegexOptions.ExplicitCapture);
        static public Regex regexCollection = new Regex(@"^/collections/(?<COLID>[^/?#]+)(/.*)?$", RegexOptions.ExplicitCapture);
        static public Regex regexAuthor = new Regex(@"^/users/(?<USERNAME>[^/?#]+)(/.*)?$", RegexOptions.ExplicitCapture);
        static Regex regexRSSTagTitle = new Regex(@"AO3 works tagged '(?<TAGNAME>.*)'$", RegexOptions.ExplicitCapture);
        static Regex regexTagCategory = new Regex(@"This tag belongs to the (?<CATEGORY>\w*) Category\.", RegexOptions.ExplicitCapture);
        static Regex regexPageQuery = new Regex(@"(?<PAGE>&?page=\d+&?)");
        static Regex regexChapNumName = new Regex(@"^(?<CHAPTERNUM>\d+)\.\s*(?<CHAPTERNAME>.*)\s*$", RegexOptions.ExplicitCapture);
        static HttpClient HttpClient { get; set; }

        static Ao3SiteDataLookup()
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                //httpClientHandler.MaxConnectionsPerServer = 8;
                MaxRequestContentBufferSize = 1 << 20
            };
            HttpClient = new HttpClient(httpClientHandler);

            HtmlNode.ElementsFlags["option"] = HtmlElementFlag.Empty | HtmlElementFlag.Closed;
            HtmlNode.ElementsFlags["dd"] = HtmlElementFlag.Empty | HtmlElementFlag.Closed;
            HtmlNode.ElementsFlags["dt"] = HtmlElementFlag.Empty | HtmlElementFlag.Closed;
            httpSemaphore = new SemaphoreSlim(5);
        }

        static SemaphoreSlim httpSemaphore;

        static Task<HttpResponseMessage> HttpRequestAsync(Uri uri, HttpMethod method = null, string mediaType = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, string cookies = null)
        {
            return Task.Run(async () =>
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        HttpRequestMessage message = new HttpRequestMessage(method ?? HttpMethod.Get, uri);
                        if (!string.IsNullOrEmpty(mediaType)) message.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(mediaType));
                        if (!string.IsNullOrWhiteSpace(cookies)) message.Headers.Add("Cookie", cookies);

                        await httpSemaphore.WaitAsync();
                        var task = HttpClient.SendAsync(message, completionOption);
                        return await task;
                    }
                    catch (HttpRequestException e)
                    {
                        if (i == 2)
                        {
                            if (e.InnerException is WebException webexp)
                            {
                                WebViewPage.Current.ShowError("Error while getting data from Ao3: " + webexp.Status);
                            }
                            else if (e.InnerException is System.Runtime.InteropServices.COMException comexp && comexp.Data.Contains("RestrictedDescription") && !string.IsNullOrWhiteSpace(comexp.Data["RestrictedDescription"] as string))
                            {
                                WebViewPage.Current.ShowError("Error while getting data from Ao3: " + comexp.Data["RestrictedDescription"] as string);
                            }
                            else
                            {
                                WebViewPage.Current.ShowError("Error while getting data from Ao3:" + e.Message);
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception e)
                    {
                        if (i == 2)
                        {
                            WebViewPage.Current.ShowError("Error while getting data from Ao3: " + e.Message);
                            App.Log(e);
                        }
                    }
                    finally
                    {
                        httpSemaphore.Release();
                    }
                }
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            });
        }

        class WorkWorker
        {
            static object workSummaryLock = new object();
            static List<WorkWorker> workLookupWorkers = new List<WorkWorker>();

            SemaphoreSlim start;
            SemaphoreSlim finished;
            HtmlDocument doc;
            List<long> works;

            public WorkWorker()
            {
                start = new SemaphoreSlim(0, 1);
                finished = new SemaphoreSlim(0, 10);
                works = new List<long>();
            }

            bool Full { get { return works.Count >= 10; } }
            bool started = false;
            void Reset()
            {
                while (start.CurrentCount != 0) start.Wait();
                while (finished.CurrentCount != 0) finished.Wait();
                doc = null;
                works.Clear();
                started = false;
            }
            async Task<HtmlDocument> Wait()
            {
                lock (workSummaryLock)
                {
                    if (Full)
                    {
                        if (workLookupWorkers.Remove(this))
                            start.Release();
                    }

                    if (!started)
                    {
                        started = true;
                        Task.Run(async () =>
                        {
                            lock (workSummaryLock)
                            {
                            }

                            await start.WaitAsync(1000);

                            lock (workSummaryLock)
                            {
                                workLookupWorkers.Remove(this);
                            }

                            var wsuri = new Uri(@"https://archiveofourown.org/works/search?utf8=%E2%9C%93&work_search%5Bquery%5D=id%3A%28" + string.Join("+OR+", works) + "%29");
                            var response = await HttpRequestAsync(wsuri);

                            if (response.IsSuccessStatusCode)
                            {
                                doc = await response.Content.ReadAsHtmlDocumentAsync();
                            }

                            finished.Release(works.Count);
                        });
                    }
                }

                await finished.WaitAsync();

                return doc;
            }

            public static async Task<HtmlNode> LookupSummaryAsync(long workid)
            {
                WorkWorker worker = null;
                lock (workSummaryLock)
                {
                    for (var i = 0; i < workLookupWorkers.Count; i++)
                    {
                        if (!workLookupWorkers[i].Full)
                        {
                            worker = workLookupWorkers[i];
                            break;
                        }
                    }

                    if (worker is null) workLookupWorkers.Add(worker = new WorkWorker());

                    worker.works.Add(workid);
                }

                var doc = await worker.Wait();

                lock (workSummaryLock)
                {
                    if (worker.works.Remove(workid) && worker.works.Count == 0)
                    {
                        worker.Reset();
                        workLookupWorkers.Add(worker);
                    }
                }

                var worknode = doc.GetElementbyId("work_" + workid.ToString());
                return worknode;
            }
        }

        static string EscapeTag(string tag)
        {
            if (tag is null) return null;
            return regexEscTag.Replace(tag, (match) =>
            {
                int i = Array.IndexOf(usescTagStrings, match.Value);
                if (i != -1) return escTagStrings[i];
                return "";
            });
        }

        static public string UnescapeTag(string tag)
        {
            if (tag is null) return null;
            return regexUnescTag.Replace(tag, (match) =>
            {
                int i = Array.IndexOf(escTagStrings, match.Value);
                if (i != -1) return usescTagStrings[i];
                return "";
            }).PoolString();
        }

        static public string LookupTagQuick(int tagid)
        {
            string tag = App.Database.GetTag(tagid);
            if (!string.IsNullOrEmpty(tag))
                return tag.PoolString();

            return null;
        }  

        static public async Task<string> LookupTagAsync(int tagid)
        {
            string tag = App.Database.GetTag(tagid);
            if (!string.IsNullOrEmpty(tag))
                return tag;

            tag = null;
            var uri = new Uri(@"https://archiveofourown.org/tags/feed/" + tagid.ToString());

            var response = await HttpRequestAsync(uri, mediaType: "application/atom+xml", completionOption: HttpCompletionOption.ResponseHeadersRead);

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
                XmlReaderSettings settings = new XmlReaderSettings()
                {
                    IgnoreWhitespace = true,
                    IgnoreComments = true
                };
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
            tag = tag.PoolString();
            if (!(tag is null)) App.Database.SetTagId(tag, tagid);
            return tag;
        }

        static public TagCache LookupTagQuick(string intag, bool ignoreexpires = false)
        {
            intag = UnescapeTag(intag);
            var tag = App.Database.GetTag(intag, ignoreexpires) ?? new TagCache { name = intag };
            if (!string.IsNullOrEmpty(tag.actual))
            {
                return tag;
            }
            return null;
        }

        static SemaphoreSlim taglockslock = new SemaphoreSlim(1);
        static Dictionary<string, SemaphoreSlim> taglocks = new Dictionary<string, SemaphoreSlim>();
        static Stack<SemaphoreSlim> oldtaglocks = new Stack<SemaphoreSlim>();
        static SemaphoreSlim GetTagLockSem()
        {
            if (oldtaglocks.Count != 0) return oldtaglocks.Pop();
            else return new SemaphoreSlim(1);
        }
        static void DisposeTagLockSem(SemaphoreSlim sem)
        {
            oldtaglocks.Push(sem);
        }

        static public async Task<TagCache> LookupTagAsync(string intag, string synallow = null)
        {
            if (string.IsNullOrWhiteSpace(intag)) return new TagCache { name = intag, actual = intag };

            intag = UnescapeTag(intag);
            var tag = App.Database.GetTag(intag);
            if (!string.IsNullOrEmpty(tag?.actual))
            {
                return tag;
            }

            // Get a lock for this tag
            SemaphoreSlim taglock;
            using (await taglockslock.LockAsync())
            {
                if (!taglocks.TryGetValue(intag, out taglock))
                    taglocks[intag] = taglock = GetTagLockSem();
            }

            await taglock.LockAsync();
            try
            {
                tag = App.Database.GetTag(intag) ?? new TagCache { name = intag };
                if (!string.IsNullOrEmpty(tag?.actual))
                {
                    return tag;
                }

                tag.actual = intag.PoolString();

                var uri = new Uri(@"https://archiveofourown.org/tags/" + EscapeTag(intag));

                var response = await HttpRequestAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    HtmlDocument doc = await response.Content.ReadAsHtmlDocumentAsync();
                    var main = doc.GetElementbyId("main");

                    HtmlNode tagnode = main?.ElementByClass("div", "tag");
                    if (!(tagnode is null))
                    {
                        // Merger?
                        HtmlNode mergernode = tagnode.ElementByClass("div", "merger");
                        if (!(mergernode is null))
                        {
                            foreach (var e in mergernode.DescendantsByClass("a", "tag"))
                            {
                                var href = e.Attributes["href"];
                                if (!(href is null) && !string.IsNullOrEmpty(href.Value))
                                {
                                    var newuri = new Uri(uri, href.Value.HtmlDecode());
                                    var m = regexTag.Match(newuri.LocalPath);
                                    if (m.Success)
                                    {
                                        var merger = UnescapeTag(m.Groups["TAGNAME"].Value);
                                        var mergertag = await LookupTagAsync(merger, intag);

                                        var frommerger = App.Database.GetTag(intag);
                                        if (!string.IsNullOrEmpty(frommerger?.actual))
                                            return frommerger;

                                        // Unlucky we hit a race condition where the merger was already being looked up 
                                        // and our lock on ourselves blocked it from writing this tags details to the db
                                        // Is a small performance loss having to do it here
                                        tag.actual = merger.PoolString();
                                        if (mergertag != null)
                                        {
                                            tag.parentsStr = mergertag.parentsStr;
                                            tag.category = mergertag.category;
                                            App.Database.SetTagDetails(tag);
                                            return tag;
                                        }
                                    }
                                    break;
                                }
                            }
                        }

                        // Parents
                        HtmlNode parenttagsnode = tagnode.ElementByClass("div", "parent")?.Element("ul");
                        if (!(parenttagsnode is null))
                        {
                            foreach (var e in parenttagsnode.DescendantsByClass("a", "tag"))
                            {
                                var href = e.Attributes["href"];
                                if (!(href is null) && !string.IsNullOrEmpty(href.Value))
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
                            if (!(p.InnerText is null))
                            {
                                var m = regexTagCategory.Match(p.InnerText.HtmlDecode());
                                if (m.Success)
                                {
                                    tag.category = m.Groups["CATEGORY"].Value;
                                    break;
                                }
                            }
                        }

                        TaskCompletionSource<object> wait = new TaskCompletionSource<object>();
                        await Task.Run(() =>
                        {
                            // All txn commands must be on same thread or deadlock!
                            using (App.Database.DoTransaction())
                            {
                                App.Database.SetTagDetails(tag);
                                wait.SetResult(null);

                                // synonyms
                                HtmlNode synonymstagsnode = tagnode.ElementByClass("div", "synonym")?.Element("ul");
                                if (!(synonymstagsnode is null))
                                {
                                    var links = synonymstagsnode.DescendantsByClass("a", "tag");
                                    foreach (var e in links)
                                    {
                                        var href = e.Attributes["href"];
                                        if (!(href is null) && !string.IsNullOrEmpty(href.Value))
                                        {
                                            var newuri = new Uri(uri, href.Value.HtmlDecode());
                                            var m = regexTag.Match(newuri.LocalPath);
                                            if (m.Success && !string.IsNullOrWhiteSpace(m.Groups["TAGNAME"].Value))
                                            {
                                                var ssyntag = UnescapeTag(m.Groups["TAGNAME"].Value);

                                                SemaphoreSlim syntaglock = null;
                                                if (ssyntag != synallow) using (taglockslock.Lock())
                                                    {
                                                        if (!taglocks.TryGetValue(ssyntag, out syntaglock))
                                                            taglocks[ssyntag] = syntaglock = GetTagLockSem();
                                                    }

                                                // If the lock is already held then another thread is already working on this, we wont even attempt to do anything to it
                                                if (syntaglock?.Wait(0) != false)
                                                {
                                                    try
                                                    {
                                                        var syntag = App.Database.GetTag(ssyntag) ?? new TagCache { name = ssyntag };
                                                        if (string.IsNullOrEmpty(syntag?.actual))
                                                        {
                                                            syntag.actual = tag.actual.PoolString();
                                                            syntag.parents = tag.parents;
                                                            syntag.category = tag.category;
                                                            App.Database.SetTagDetails(syntag);
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        syntaglock?.Release();
                                                    }
                                                }

                                                // Release lock and delete if no thread waiting

                                                if (!(syntaglock is null)) using (taglockslock.Lock())
                                                    {
                                                        if (syntaglock.CurrentCount == 1)
                                                        {
                                                            DisposeTagLockSem(syntaglock);
                                                            taglocks.Remove(ssyntag);
                                                        }
                                                    }
                                            }
                                        }
                                    }
                                }
                            }
                        }).ConfigureAwait(false);

                        await wait.Task;
                    }

                }
            }
            finally
            {
                taglock.Release();

                // Release lock and delete if no thread waiting
                using (await taglockslock.LockAsync())
                {
                    if (taglock.CurrentCount == 1)
                    {
                        DisposeTagLockSem(taglock);
                        taglocks.Remove(intag);
                    }
                }
            }

            return tag;
        }

        static public Ao3TagType GetTypeForCategory(string category)
        {
            if (!string.IsNullOrEmpty(category))
            {
                if (Enum.TryParse(category, true, out Ao3TagType type) || Enum.TryParse(category + "s", true, out type))
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

            name = await FetchSearchLanguagesSortColumns(langid: langid);

            return name;
        }
        static public string LookupSortColumnQuick(string sortcolumn)
        {
            string name = App.Database.GetSortColumn(sortcolumn);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return null;
        }

        static public async Task<string> LookupSortColumnAsync(string sortcolumn)
        {
            string name = App.Database.GetSortColumn(sortcolumn);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            name = await FetchSearchLanguagesSortColumns(sortcolumn: sortcolumn);

            return name;
        }

        static async Task<string> FetchSearchLanguagesSortColumns(int? langid = null, string sortcolumn = null)
        {
            string name = null;

            var uri = new Uri(@"https://archiveofourown.org/works/search");

            var response = await HttpRequestAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                HtmlDocument doc = await response.Content.ReadAsHtmlDocumentAsync();

                var select = doc.GetElementbyId("work_search_language_id");
                foreach (var opt in select.Elements("option"))
                {
                    var value = opt.Attributes["value"];

                    if (!(value is null) && !string.IsNullOrEmpty(value.Value) && !string.IsNullOrWhiteSpace(opt.InnerText))
                    {
                        if (int.TryParse(value.Value, out int i))
                        {
                            var n = opt.InnerText.HtmlDecode().Trim();
                            App.Database.SetLanguage(n, i);
                            if (i == langid) name = n;
                        }
                    }

                }


                select = doc.GetElementbyId("work_search_sort_column");
                foreach (var opt in select.Elements("option"))
                {
                    var value = opt.Attributes["value"];

                    if (!(value is null) && !string.IsNullOrEmpty(value.Value) && !string.IsNullOrWhiteSpace(opt.InnerText))
                    {
                        string i = value.Value.HtmlDecode().Trim();
                        var n = opt.InnerText.HtmlDecode().Trim();
                        App.Database.SetSortColumn(n, i);
                        if (i == sortcolumn) name = n;
                    }
                }
            }

            return name;
        }

        public static Uri CheckUri(Uri uri)
        {
            if (uri.Host == "archiveofourown.org" || uri.Host == "www.archiveofourown.org")
            {
                if (uri.Scheme == "http" || uri.Port != -1 || uri.Host == "www.archiveofourown.org")
                {
                    var uribuilder = new UriBuilder(uri)
                    {
                        Host = "archiveofourown.org",
                        Scheme = "https",
                        Port = -1
                    };
                    uri = uribuilder.Uri;
                }
                return uri;
            }
            return null;
        }

        public static Uri ReadingListlUri(string url)
        {
            var uribuilder = new UriBuilder(regexPageQuery.Replace(url, (m) =>
            {
                if (m.Value.StartsWith("&") && m.Value.EndsWith("&")) return "&";
                else return "";
            }).TrimEnd('?'));

            if (uribuilder.Host == "archiveofourown.org" || uribuilder.Host == "www.archiveofourown.org")
            {
                uribuilder.Host = "archiveofourown.org";
                uribuilder.Scheme = "http";
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
            else if ((match = regexCollection.Match(uri.LocalPath)).Success)
            {
                var sCOLID = match.Groups["COLID"].Value;
                uri = new Uri(uri, "/collections/" + sCOLID);
            }

            return uri;
        }
        public static IDictionary<string, Ao3PageModel> LookupQuick(ICollection<string> urls)
        {
            var dict = new Dictionary<string, Ao3PageModel>(urls.Count);

            foreach (string url in urls)
            {
                dict[url] = LookupQuick(url);
            }
            return dict;
        }

        public static Ao3PageModel LookupQuick(string url)
        {
            var uribuilder = new UriBuilder(regexPageQuery.Replace(url, (m) =>
            {
                if (m.Value.StartsWith("&") && m.Value.EndsWith("&")) return "&";
                else return "";
            }).TrimEnd('?'));

            if (uribuilder.Host == "archiveofourown.org" || uribuilder.Host == "www.archiveofourown.org")
            {
                uribuilder.Host = "archiveofourown.org";
                uribuilder.Scheme = "http";
                uribuilder.Port = -1;
            }
            else
            {
                return null;
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
            else if (uri.LocalPath == "/works/search") // Work search and Advanced search
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
                model.PrimaryTag = tagdetails?.actual ?? UnescapeTag(sTAGNAME);
                model.PrimaryTagType = GetTypeForCategory(tagdetails?.category);
                if (!(tagdetails is null))
                {
                    SortedDictionary<Ao3TagType, List<string>> tags = model.Tags = new SortedDictionary<Ao3TagType, List<string>>();
                    foreach (string ptag in tagdetails.parents)
                    {
                        var ptagdetails = LookupTagQuick(ptag);

                        var actual = ptagdetails?.actual ?? ptag;
                        var tt = GetTypeForCategory(ptagdetails?.category);

                        if (!tags.TryGetValue(tt, out List<string> list))
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
            else if ((match = regexSeries.Match(uri.LocalPath)).Success)
            {
                model.Type = Ao3PageType.Series;
                model.PrimaryTag = "<Series>";
                model.PrimaryTagType = Ao3TagType.Other;

            }
            else if ((match = regexCollection.Match(uri.LocalPath)).Success)
            {
                var sCOLID = match.Groups["COLID"].Value;
                model.Uri = uri = new Uri(uri, "/collections/" + sCOLID);
                model.Type = Ao3PageType.Collection;
                model.Title = sCOLID;
                model.PrimaryTag = "<Collection>";
                model.PrimaryTagType = Ao3TagType.Other;
            }
            else
            {
                model.Type = Ao3PageType.Unknown;
                if (uri.LocalPath == "/") model.Title = "Archive of Our Own Home Page";
                //model.Title = uri.ToString();
            }

            return model;
        }

        public static async Task<Ao3PageModel> LookupAsync(string url)
        {
            var uribuilder = new UriBuilder(regexPageQuery.Replace(url, (m) =>
            {
                if (m.Value.StartsWith("&") && m.Value.EndsWith("&")) return "&";
                else return "";
            }).TrimEnd('?'));

            if (uribuilder.Host == "archiveofourown.org" || uribuilder.Host == "www.archiveofourown.org")
            {
                uribuilder.Host = "archiveofourown.org";
                uribuilder.Scheme = "http";
                uribuilder.Port = -1;
            }
            else
            {
                return null;
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
            else if (uri.LocalPath == "/works/search") // Work search and Advanced search
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

                    if (!tags.TryGetValue(t.Key, out List<string> list))
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
                model.Uri = uri = new Uri(uri, "/works/" + sWORKID);

                model.Details = new Ao3WorkDetails()
                {
                    WorkId = long.Parse(sWORKID)
                };
                var wsuri = new Uri(@"https://archiveofourown.org/works/search?utf8=%E2%9C%93&work_search%5Bquery%5D=id%3A" + sWORKID);

                var worknode = await WorkWorker.LookupSummaryAsync(model.Details.WorkId);

                if (worknode is null)
                {
                    // No worknode, try with cookies
                    string cookies = App.Database.GetVariable("siteCookies");
                    if (!string.IsNullOrWhiteSpace(cookies))
                    {
                        var response = await HttpRequestAsync(wsuri, cookies: cookies);

                        if (response.IsSuccessStatusCode)
                        {
                            var doc = await response.Content.ReadAsHtmlDocumentAsync();
                            worknode = doc.GetElementbyId("work_" + sWORKID);
                        }
                    }
                }

                if (!(worknode is null)) await FillModelFromWorkSummaryAsync(wsuri, worknode, model);
            }
            else if ((match = regexSeries.Match(uri.LocalPath)).Success)
            {
                model.Type = Ao3PageType.Series;

                // Only way to get data is from the page itself
                var response = await HttpRequestAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    HtmlDocument doc = await response.Content.ReadAsHtmlDocumentAsync();

                    var main = doc.GetElementbyId("main");

                    if (!(main is null))
                    {
                        var title = main.ElementByClass("h2", "heading");
                        model.Title = title?.InnerText?.HtmlDecode()?.Trim();

                        model.Details = new Ao3WorkDetails();
                        await FillSeriesAsync(uri, main, model);
                    }
                }
            }
            else if ((match = regexCollection.Match(uri.LocalPath)).Success)
            {
                var sCOLID = match.Groups["COLID"].Value;
                model.Uri = new Uri(uri, "/collections/" + sCOLID);
                uri = new Uri(uri, "/collections/" + sCOLID + "/");
                model.Type = Ao3PageType.Collection;

                // Only way to get data is from the page itself
                var response = await HttpRequestAsync(new Uri(uri, "profile"));

                if (response.IsSuccessStatusCode)
                {
                    HtmlDocument doc = await response.Content.ReadAsHtmlDocumentAsync();

                    var colnode = doc.GetElementbyId("main")?.ElementByClass("div", "collection");

                    if (!(colnode is null))
                    {
                        var title = colnode.ElementByClass("div", "header")?.ElementByClass("h2", "heading");
                        model.Title = title?.InnerText?.HtmlDecode()?.Trim();

                        model.Details = new Ao3WorkDetails();
                        await FillCollectionAsync(uri, colnode, model);
                    }
                }

            }
            else
            {
                model.Type = Ao3PageType.Unknown;
                if (uri.LocalPath == "/") model.Title = "Archive of Our Own Home Page";
                //model.Title = uri.ToString();
            }
            return model;
        }

        public static bool TryGetWorkId(this Uri uri, out long workid)
        {
            workid = 0;
            var match = regexWork.Match(uri.LocalPath);
            return uri.Host == "archiveofourown.org" && match.Success && long.TryParse(match.Groups["WORKID"].Value, out workid);
        }

        private static async Task FillModelFromWorkSummaryAsync(Uri baseuri, HtmlNode worknode, Ao3PageModel model)
        {
            // Gather all tags
            SortedDictionary<Ao3TagType, List<string>> tags = model.Tags = new SortedDictionary<Ao3TagType, List<string>>();

            // Tags node
            HtmlNode tagsnode = worknode?.ElementByClass("ul", "tags");

            if (!(tagsnode is null))
            {
                foreach (var tn in tagsnode.Elements("li"))
                {
                    var a = tn.DescendantsByClass("a", "tag").FirstOrDefault();
                    if (a is null) continue;

                    Ao3TagType type = Ao3TagType.Other;

                    foreach (var c in tn.GetClasses())
                    {
                        if (Enum.TryParse(c, true, out type))
                        {
                            break;
                        }
                    }

                    var href = a.Attributes["href"];
                    if (!(href is null) && !string.IsNullOrEmpty(href.Value))
                    {
                        var reluri = new Uri(baseuri, href.Value.HtmlDecode());
                        var m = regexTag.Match(reluri.LocalPath);
                        string tag;
                        if (m.Success)
                        {
                            tag = m.Groups["TAGNAME"].Value;
                        }
                        else
                        {
                            tag = a.InnerText.HtmlDecode();
                        }

                        if (!tags.TryGetValue(type, out List<string> list)) tags[type] = list = new List<string>();
                        list.Add(UnescapeTag(tag));
                    }
                }
            }

            // Header
            var headernode = worknode?.ElementByClass("div", "header");

            // Get Fandom tags
            HtmlNode fandomnode = headernode?.ElementByClass("fandoms");
            if (!(fandomnode is null))
            {
                foreach (var a in fandomnode.Elements("a"))
                {
                    var href = a.Attributes["href"];
                    if (!(href is null) && !string.IsNullOrEmpty(href.Value))
                    {
                        var reluri = new Uri(baseuri, href.Value.HtmlDecode());
                        var m = regexTag.Match(reluri.LocalPath);
                        string tag;
                        if (m.Success)
                            tag = m.Groups["TAGNAME"].Value;
                        else
                            tag = a.InnerText.HtmlDecode();

                        if (!tags.TryGetValue(Ao3TagType.Fandoms, out List<string> list)) tags[Ao3TagType.Fandoms] = list = new List<string>();
                        list.Add(UnescapeTag(tag));
                    }
                }
            }

            HtmlNode headingnode = headernode?.ElementByClass("heading");
            if (!(headingnode is null))
            {
                var links = headingnode.Elements("a");
                Dictionary<string, string> authors = new Dictionary<string, string>(1);
                Dictionary<string, string> recipiants = new Dictionary<string, string>();
                if (!(links is null))
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
            Dictionary<Ao3RequiredTag, HtmlNode> required = new Dictionary<Ao3RequiredTag, HtmlNode>(4)
            {
                [Ao3RequiredTag.Rating] = requirednode?.DescendantsByClass("span", "rating")?.FirstOrDefault(),
                [Ao3RequiredTag.Warnings] = requirednode?.DescendantsByClass("span", "warnings")?.FirstOrDefault(),
                [Ao3RequiredTag.Category] = requirednode?.DescendantsByClass("span", "category")?.FirstOrDefault(),
                [Ao3RequiredTag.Complete] = requirednode?.DescendantsByClass("span", "iswip")?.FirstOrDefault()
            };
            model.RequiredTags = new Dictionary<Ao3RequiredTag, Ao3RequredTagData>(4);
            foreach (var n in required)
            {
                if (n.Value is null)
                {
                    model.RequiredTags[n.Key] = null;
                    continue;
                }

                // Add req tags to the tag listing
                if (n.Key != Ao3RequiredTag.Warnings)
                {
                    var type = RequiredTagToType[n.Key];
                    foreach (var t in n.Value.GetAttributeValue("title", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var unescaped = UnescapeTag(t.Trim());
                        if (string.IsNullOrWhiteSpace(unescaped)) continue;
                        if (!tags.TryGetValue(type, out List<string> list)) tags[type] = list = new List<string>();
                        if (!list.Contains(unescaped)) list.Add(unescaped);
                    }
                }

                var classes = n.Value.GetClasses();
                var search = n.Key.ToString();
                var tag = Array.Find(classes, (val) =>
                {
                    return val.StartsWith(search + "-", StringComparison.OrdinalIgnoreCase) || val.StartsWith(search.TrimEnd('s') + "-", StringComparison.OrdinalIgnoreCase);
                });

                if (tag is null)
                    model.RequiredTags[n.Key] = null;
                else
                    model.RequiredTags[n.Key] = new Ao3RequredTagData(tag, n.Value.InnerText.HtmlDecode().Trim());

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

            if (!(model.PrimaryTag is null))
            {
                var tagdetails = await LookupTagAsync(model.PrimaryTag);
                model.PrimaryTag = tagdetails.actual;
                model.PrimaryTagType = GetTypeForCategory(tagdetails.category);
            }

            // Stats

            var stats = worknode?.ElementByClass("dl", "stats");

            var updatedate = headernode?.ElementByClass("p", "datetime")?.InnerText?.HtmlDecode()?.Trim();
            if (!string.IsNullOrEmpty(updatedate) && DateTime.TryParseExact(updatedate, "d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime datetime))
            {
                model.Details.LastUpdated = datetime;
            }

            model.Language = stats?.ElementByClass("dd", "language")?.InnerText?.HtmlDecode()?.Trim();

#pragma warning disable IDE0018 // Inline variable declaration
            int intval;
#pragma warning restore IDE0018 // Inline variable declaration
            if (!(stats is null) && int.TryParse(stats.ElementByClass("dd", "words")?.InnerText?.HtmlDecode()?.Replace(",", ""), out intval))
                model.Details.Words = intval;
            if (!(stats is null) && int.TryParse(stats.ElementByClass("dd", "collections")?.InnerText?.HtmlDecode(), out intval))
                model.Details.Collections = intval;
            if (!(stats is null) && int.TryParse(stats.ElementByClass("dd", "comments")?.InnerText?.HtmlDecode(), out intval))
                model.Details.Comments = intval;
            if (!(stats is null) && int.TryParse(stats.ElementByClass("dd", "kudos")?.InnerText?.HtmlDecode(), out intval))
                model.Details.Kudos = intval;
            if (!(stats is null) && int.TryParse(stats.ElementByClass("dd", "bookmarks")?.InnerText?.HtmlDecode(), out intval))
                model.Details.Bookmarks = intval;
            if (!(stats is null) && int.TryParse(stats.ElementByClass("dd", "hits")?.InnerText?.HtmlDecode(), out intval))
                model.Details.Hits = intval;

            // Series

            var seriesnode = worknode?.ElementByClass("ul", "series");
            if (!(seriesnode is null))
            {
                Dictionary<string, Ao3SeriesLink> series = new Dictionary<string, Ao3SeriesLink>(1);
                foreach (var n in seriesnode.Elements("li"))
                {
                    var link = n.Element("a");
                    if ((link is null) || String.IsNullOrWhiteSpace(link.InnerText)) continue;

                    var s = link.Attributes["href"]?.Value;
                    if (String.IsNullOrWhiteSpace(s)) continue;
                    var uri = new Uri(baseuri, s.HtmlDecode());

                    var part = n.Element("strong")?.InnerText?.HtmlDecode();
                    if (String.IsNullOrWhiteSpace(part)) continue;

                    if (int.TryParse(part, out intval))
                        series[uri.AbsoluteUri] = new Ao3SeriesLink(intval, link.InnerText?.HtmlDecode());

                }
                if (series.Count > 0) model.Details.Series = series;
            }

            var chapters = stats?.ElementByClass("dd", "chapters")?.InnerText?.Trim()?.Split('/');
            if (!(chapters is null))
            {
                int? total;
                if (chapters[1] == "?") total = null;
                else total = int.Parse(chapters[1]);

                if (int.TryParse(chapters[0], out intval))
                    model.Details.Chapters = new Ao3ChapterDetails(intval, total);
            }

            // Horrible horrible dirty grabbing of the summary
            var summarynode = worknode?.ElementByClass("blockquote", "summary");
            if (!(summarynode is null))
            {

                try
                {
                    model.Details.Summary = Text.HtmlConverter.ConvertNode(summarynode);
                }
                catch (Exception)
                {

                }
            }
        }

        private static List<Task<Ao3PageModel>> GatherWorksAsync(Uri baseuri, HtmlNode workstag, Ao3PageModel model)
        {
            var worktasks = new List<Task<Ao3PageModel>>(workstag.ChildNodes.Count);
            foreach (var worknode in workstag.ChildNodes)
            {
                if (!worknode.Id.StartsWith("work_"))
                    continue;

                string sWORKID = worknode.Id.Substring(5);
                int workid = int.Parse(sWORKID);

                worktasks.Add(Task.Run(async () =>
                {
                    Ao3PageModel workmodel = new Ao3PageModel
                    {
                        Uri = new Uri(baseuri, "/works/" + sWORKID),
                        Type = Ao3PageType.Work
                    };
                    workmodel.Details = new Ao3WorkDetails()
                    {
                        WorkId = long.Parse(sWORKID)
                    };
                    await FillModelFromWorkSummaryAsync(baseuri, worknode, workmodel);

                    return workmodel;
                }));
            }
            return worktasks;
        }

        private static void FillInfoFromWorkModels(Ao3PageModel model)
        {
            // Gather all tags
            var tags = model.Tags = new SortedDictionary<Ao3TagType, List<string>>();
            var primaries = new Dictionary<string, double>();
            var tagtypes = new Dictionary<string, Ao3TagType>();
            var languages = new HashSet<string>();
            var reqtags = new Dictionary<string, string>();

            if (model.RequiredTags is null) model.RequiredTags = new Dictionary<Ao3RequiredTag, Ao3RequredTagData>(4);

            DateTime updated = DateTime.MinValue;
            int words = 0;
            int chapsavail = 0;
            int? chapstotal = null;
            if (model.RequiredTags.TryGetValue(Ao3RequiredTag.Complete, out var crt) && crt.Tag == "complete-yes")
                chapstotal = 0;

            foreach (var workmodel in model.SeriesWorks)
            {
                // Coalate tags
                foreach (var kvp in workmodel.Tags)
                {
                    if (!tags.TryGetValue(kvp.Key, out var list)) tags[kvp.Key] = list = new List<string>();

                    foreach (var tag in kvp.Value)
                    {
                        if (!list.Contains(tag))
                            list.Add(tag);
                    }
                }
                // Coalate required tags
                foreach (var kvp in RequiredTagToType)
                {
                    if (workmodel.RequiredTags.TryGetValue(kvp.Key, out var reqTag))
                    {
                        if (!tags.TryGetValue(kvp.Value, out List<string> list))
                        {
                            reqtags[reqTag.Label] = reqTag.Tag;
                            tags[kvp.Value] = list = new List<string>();
                        }

                        foreach (var separated in reqTag.Label.Split(','))
                        {
                            var s = separated.Trim();
                            if (s != "" && !list.Contains(s))
                                list.Add(s);
                        }
                    }
                }

                // Count each primary tag
                primaries.TryGetValue(workmodel.PrimaryTag, out var ptc);
                ptc += workmodel.PrimaryTag.Contains("/") ? 1.1 : 1.0;
                primaries[workmodel.PrimaryTag] = ptc;
                tagtypes[workmodel.PrimaryTag] = workmodel.PrimaryTagType;

                languages.Add(workmodel.Language);

                chapsavail += workmodel.Details.Chapters.Available;
                if (workmodel.Details.Chapters.Total is null) chapstotal = null;
                else if (!(chapstotal is null)) chapstotal += workmodel.Details.Chapters.Total;

                if (!(workmodel.Details.Words is null)) words += (int)workmodel.Details.Words;

                if (!(workmodel.Details.LastUpdated is null) && updated < workmodel.Details.LastUpdated) updated = (DateTime)workmodel.Details.LastUpdated;
            }

            // Generate required tags
#pragma warning disable IDE0018 // Inline variable declaration
            List<string> req;
#pragma warning restore IDE0018 // Inline variable declaration
            if (tags.TryGetValue(Ao3TagType.Warnings, out req))
            {
                if (req.Count == 1 && reqtags.TryGetValue(req[0], out string sclass))
                    model.RequiredTags[Ao3RequiredTag.Warnings] = new Ao3RequredTagData(sclass, req[0]);
                else
                    model.RequiredTags[Ao3RequiredTag.Warnings] = new Ao3RequredTagData("warning-yes", string.Join(", ", req));
            }
            if (tags.TryGetValue(Ao3TagType.Category, out req))
            {
                if (req.Count == 1 && reqtags.TryGetValue(req[0], out string sclass))
                    model.RequiredTags[Ao3RequiredTag.Category] = new Ao3RequredTagData(sclass, req[0]);
                else
                    model.RequiredTags[Ao3RequiredTag.Category] = new Ao3RequredTagData("category-multi", string.Join(", ", req));
            }
            if (tags.TryGetValue(Ao3TagType.Rating, out req))
            {
                if (req.Count == 1 && reqtags.TryGetValue(req[0], out string sclass))
                    model.RequiredTags[Ao3RequiredTag.Rating] = new Ao3RequredTagData(sclass, req[0]);
                else
                    model.RequiredTags[Ao3RequiredTag.Rating] = new Ao3RequredTagData("rating-na", string.Join(", ", req));
            }

            if (model.Details.Words is null) model.Details.Words = words;

            model.Details.Chapters = new Ao3ChapterDetails(chapsavail, chapstotal);
            model.Language = string.Join(", ", languages);

            model.PrimaryTag = primaries.OrderByDescending(kvp => kvp.Value).First().Key;
            model.PrimaryTagType = tagtypes[model.PrimaryTag];

            if (!(model.Details.LastUpdated is null) && updated > DateTime.MinValue)
                model.Details.LastUpdated = updated;
        }

        private static async Task FillSeriesAsync(Uri baseuri, HtmlNode main, Ao3PageModel model)
        {
            var authors = new Dictionary<string, string>(1);

            var workstag = main.ElementByClass("ul", "work");
            var worktasks = GatherWorksAsync(baseuri, workstag, model);

            var meta = main.ElementByClass("div", "wrapper")?.ElementByClass("dl", "meta");
            model.RequiredTags = new Dictionary<Ao3RequiredTag, Ao3RequredTagData>(4);
            if (!(meta is null))
            {
                foreach (var dt in meta.Elements("dt"))
                {
                    var dd = dt.NextSibling;
                    if (dd is null) continue;
                    while (dd.Name == "#text") dd = dd.NextSibling;
                    if (dd.Name != "dd") continue;

                    switch (dt.InnerText.HtmlDecode().Trim())
                    {
                        case "Creator:":
                            foreach (var a in dd.Elements("a"))
                            {
                                var href = a.Attributes["href"];
                                var uri = new Uri(baseuri, href.Value.HtmlDecode());
                                authors[uri.AbsoluteUri] = a.InnerText.HtmlDecode().Trim();
                            }
                            break;

                        case "Series Updated:":
                            {
                                var updatedate = dd.InnerText?.HtmlDecode()?.Trim();
                                if (!string.IsNullOrEmpty(updatedate) && DateTime.TryParseExact(updatedate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var datetime))
                                {
                                    model.Details.LastUpdated = datetime;
                                }
                            }
                            break;

                        case "Description:":
                            {
                                var blockquote = dd.Element("blockquote");
                                if (!(blockquote is null))
                                {
                                    try
                                    {
                                        model.Details.Summary = Text.HtmlConverter.ConvertNode(blockquote);
                                    }
                                    catch (Exception)
                                    {

                                    }
                                }
                            }
                            break;

                        case "Stats:":
                            foreach (var sdt in dd.ElementByClass("dl", "stats").Elements("dt"))
                            {
                                var sdd = sdt.NextSibling;
                                if (sdd is null) continue;
                                while (sdd.Name == "#text") sdd = sdd.NextSibling;
                                if (sdd.Name != "dd") continue;

                                int intval;
                                switch (sdt.InnerText.HtmlDecode().Trim())
                                {
                                    case "Words:":
                                        if (int.TryParse(sdd.InnerText.HtmlDecode().Replace(",", ""), out intval))
                                            model.Details.Words = intval;
                                        break;

                                    case "Works:":
                                        if (int.TryParse(sdd.InnerText.HtmlDecode(), out intval))
                                            model.Details.Works = intval;
                                        break;

                                    case "Complete:":
                                        switch (sdd.InnerText.HtmlDecode().Trim())
                                        {
                                            case "Yes":
                                                model.RequiredTags[Ao3RequiredTag.Complete] = new Ao3RequredTagData("complete-yes", "Complete");
                                                break;

                                            case "No":
                                                model.RequiredTags[Ao3RequiredTag.Complete] = new Ao3RequredTagData("complete-no", "Incomplete");
                                                break;

                                            default:
                                                break;
                                        }

                                        break;

                                    case "Bookmarks:":
                                        if (int.TryParse(sdd.InnerText.HtmlDecode(), out intval))
                                            model.Details.Bookmarks = intval;
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
            model.Details.Authors = authors;

            model.SeriesWorks = await Task.WhenAll(worktasks);

            FillInfoFromWorkModels(model);
        }

        private static async Task FillCollectionAsync(Uri baseuri, HtmlNode colnode, Ao3PageModel model)
        {
            var header = colnode.ElementByClass("div", "header");

            var userstuff = header?.ElementByClass("blockquote", "userstuff");

            try
            {
                model.Details.Summary = Text.HtmlConverter.ConvertNode(userstuff);
            }
            catch (Exception)
            {

            }

            var mods = new Dictionary<string, string>(1);
            var meta = colnode.ElementByClass("div", "wrapper")?.ElementByClass("dl", "meta");
            if (!(meta is null))
            {
                foreach (var dt in meta.Elements("dt"))
                {
                    var dd = dt.NextSibling;
                    if (dd is null) continue;
                    while (dd.Name == "#text") dd = dd.NextSibling;
                    if (dd.Name != "dd") continue;

                    switch (dt.InnerText.HtmlDecode().Trim())
                    {
                        case "Maintainers:":
                            foreach (var a in dd.Descendants("a"))
                            {
                                var href = a.Attributes["href"];
                                var uri = new Uri(baseuri, href.Value.HtmlDecode());
                                mods[uri.AbsoluteUri] = a.InnerText.HtmlDecode().Trim();
                            }
                            break;
                    }
                }
            }
            model.Details.Authors = mods;

            // Get the collections works!
            var worksuri = new Uri(baseuri, "works");
            List<Ao3PageModel> works = new List<Ao3PageModel>();
            while (!(worksuri is null))
            {
                var response = await HttpRequestAsync(worksuri);

                if (response.IsSuccessStatusCode)
                {
                    HtmlDocument doc = await response.Content.ReadAsHtmlDocumentAsync();

                    var main = doc.GetElementbyId("main");

                    if (!(main is null))
                    {
                        var workstag = main.ElementByClass("ol", "work");
                        works.AddRange(await Task.WhenAll(GatherWorksAsync(worksuri, workstag, model)));

                        var nextpage = main.ElementByClass("ol", "pagination")?.ElementByClass("li", "next")?.Element("a")?.Attributes["href"]?.Value?.HtmlDecode();
                        if (!string.IsNullOrWhiteSpace(nextpage))
                        {
                            worksuri = new Uri(worksuri, nextpage);
                            continue;
                        }

                    }
                }
                worksuri = null;
                break;
            }

            model.SeriesWorks = works;

            FillInfoFromWorkModels(model);
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


            model.RequiredTags = new Dictionary<Ao3RequiredTag, Ao3RequredTagData>(4);
            var tasks = new List<Task<KeyValuePair<Ao3TagType, Tuple<string, int>>>>();

            foreach (var i in Enum.GetValues(typeof(Ao3TagType)))
            {
                var name = "work_search[" + i.ToString().ToLowerInvariant().TrimEnd('s') + "_ids][]";
                if (query.TryGetValue(name, out var tagids))
                {
                    foreach (var s in tagids)
                    {
                        if (!int.TryParse(s, out int id)) continue;

                        tasks.Add(Task.Run(async () =>
                        {
                            string tag = await LookupTagAsync(id);
                            return new KeyValuePair<Ao3TagType, Tuple<string, int>>((Ao3TagType)i, new Tuple<string, int>(tag, id));
                        }));
                    }
                }
            }

            if (query.ContainsKey("work_search[other_tag_names]"))
            {
                foreach (var tag in query["work_search[other_tag_names]"][0].Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                        tasks.Add(Task.Run(async () =>
                        {
                            var tagdetails = await LookupTagAsync(tag);
                            return new KeyValuePair<Ao3TagType, Tuple<string, int>>(GetTypeForCategory(tagdetails.category), new Tuple<string, int>(UnescapeTag(tag), 0));
                        }));
                }

            }


            if (query.ContainsKey("work_search[language_id]"))
            {
                if (int.TryParse(query["work_search[language_id]"][0], out int id))
                {
                    tasks.Add(Task.Run(async () =>
                    {
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
                bool b = false;
                if (int.TryParse(query["work_search[complete]"][0], out int i) || bool.TryParse(query["work_search[complete]"][0], out b))
                {
                    if (i != 0 || b)
                    {
                        model.RequiredTags[Ao3RequiredTag.Complete] = new Ao3RequredTagData("complete-yes", "Complete only");
                    }
                    else
                    {
                    }
                }
            }
            if (!model.RequiredTags.ContainsKey(Ao3RequiredTag.Complete) || model.RequiredTags[Ao3RequiredTag.Complete] is null)
                model.RequiredTags[Ao3RequiredTag.Complete] = new Ao3RequredTagData("category-none", "Complete and Incomplete");

            tasks.Add(Task.Run(() =>
            {
                return new KeyValuePair<Ao3TagType, Tuple<string, int>>(Ao3TagType.Other, new Tuple<string, int>(model.RequiredTags[Ao3RequiredTag.Complete].Label, 0));
            }));

            if (query.ContainsKey("work_search[sort_column]"))
            {
                model.SortColumn = await LookupSortColumnAsync(query["work_search[sort_column]"][0]);
            }
            else
            {
                model.SortColumn = await LookupSortColumnAsync("");
            }

            if (query.ContainsKey("work_search[sort_direction]"))
            {
                switch (query["work_search[sort_direction]"][0])
                {
                    case "asc":
                        model.SortDirection = "Ascending";
                        break;
                    case "desc":
                        model.SortDirection = "Descending";
                        break;
                }
            }

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
            Dictionary<string, int> idmap = new Dictionary<string, int>(tasks.Count);
            foreach (var t in await Task.WhenAll(tasks))
            {
                if ((t.Value is null) || string.IsNullOrEmpty(t.Value.Item1))
                    continue;

                if (t.Value.Item2 != 0) idmap[t.Value.Item1] = t.Value.Item2;

                if (!tags.TryGetValue(t.Key, out var list))
                {
                    tags[t.Key] = list = new List<string>();
                }
                if (!list.Contains(t.Value.Item1))
                    list.Add(t.Value.Item1);
            }

            // Generate required tags
#pragma warning disable IDE0018 // Inline variable declaration
            List<string> req;
#pragma warning restore IDE0018 // Inline variable declaration
            if (tags.TryGetValue(Ao3TagType.Warnings, out req))
            {
                if (req.Count == 1 && idmap.TryGetValue(req[0], out int id) && TagIdToReqClass.TryGetValue(id, out string sclass))
                    model.RequiredTags[Ao3RequiredTag.Warnings] = new Ao3RequredTagData(sclass, req[0]);
                else
                    model.RequiredTags[Ao3RequiredTag.Warnings] = new Ao3RequredTagData("warning-yes", string.Join(", ", req));
            }
            if (tags.TryGetValue(Ao3TagType.Category, out req))
            {
                if (req.Count == 1 && idmap.TryGetValue(req[0], out int id) && TagIdToReqClass.TryGetValue(id, out string sclass))
                    model.RequiredTags[Ao3RequiredTag.Category] = new Ao3RequredTagData(sclass, req[0]);
                else
                    model.RequiredTags[Ao3RequiredTag.Category] = new Ao3RequredTagData("category-multi", string.Join(", ", req));
            }
            if (tags.TryGetValue(Ao3TagType.Rating, out req))
            {
                if (req.Count == 1 && idmap.TryGetValue(req[0], out int id) && TagIdToReqClass.TryGetValue(id, out string sclass))
                    model.RequiredTags[Ao3RequiredTag.Rating] = new Ao3RequredTagData(sclass, req[0]);
                else
                    model.RequiredTags[Ao3RequiredTag.Rating] = new Ao3RequredTagData("rating-na", string.Join(", ", req));
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


            model.RequiredTags = new Dictionary<Ao3RequiredTag, Ao3RequredTagData>(4);
            var tlist = new List<KeyValuePair<Ao3TagType, Tuple<string, int>>>();

            foreach (var i in Enum.GetValues(typeof(Ao3TagType)))
            {
                var name = "work_search[" + i.ToString().ToLowerInvariant().TrimEnd('s') + "_ids][]";
                if (query.TryGetValue(name, out List<string> tagids))
                {
                    foreach (var s in tagids)
                    {
                        if (!int.TryParse(s, out int id)) continue;

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
                if (int.TryParse(query["work_search[language_id]"][0], out int id))
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
                bool b = false;
                if (int.TryParse(query["work_search[complete]"][0], out int i) || bool.TryParse(query["work_search[complete]"][0], out b))
                {
                    if (i != 0 || b)
                    {
                        model.RequiredTags[Ao3RequiredTag.Complete] = new Ao3RequredTagData("complete-yes", "Complete only");
                    }
                    else
                    {
                    }
                }
            }
            if (!model.RequiredTags.ContainsKey(Ao3RequiredTag.Complete) || model.RequiredTags[Ao3RequiredTag.Complete] is null)
                model.RequiredTags[Ao3RequiredTag.Complete] = new Ao3RequredTagData("category-none", "Complete and Incomplete");

            tlist.Add(new KeyValuePair<Ao3TagType, Tuple<string, int>>(Ao3TagType.Other, new Tuple<string, int>(model.RequiredTags[Ao3RequiredTag.Complete].Label, 0)));

            if (query.ContainsKey("work_search[sort_column]"))
            {
                model.SortColumn = LookupSortColumnQuick(query["work_search[sort_column]"][0]);
            }
            else
            {
                model.SortColumn = LookupSortColumnQuick("");
            }

            if (query.ContainsKey("work_search[sort_direction]"))
            {
                switch (query["work_search[sort_direction]"][0])
                {
                    case "asc":
                        model.SortDirection = "Ascending";
                        break;
                    case "desc":
                        model.SortDirection = "Descending";
                        break;
                }
            }

            if (query.ContainsKey("work_search[query]"))
            {
                model.SearchQuery = query["work_search[query]"][0];
            }

            if (query.ContainsKey("tag_id"))
            {
                var tagdetails = LookupTagQuick(query["tag_id"][0]);
                model.PrimaryTag = tagdetails?.actual ?? UnescapeTag(query["tag_id"][0]);
                model.PrimaryTagType = GetTypeForCategory(tagdetails?.category);
            }

            // Now deal with tags that we looked up            
            SortedDictionary<Ao3TagType, List<string>> tags = model.Tags = model.Tags ?? new SortedDictionary<Ao3TagType, List<string>>();
            Dictionary<string, int> idmap = new Dictionary<string, int>(tlist.Count);
            foreach (var t in tlist)
            {
                if ((t.Value is null) || string.IsNullOrEmpty(t.Value.Item1))
                    continue;

                if (t.Value.Item2 != 0) idmap[t.Value.Item1] = t.Value.Item2;

                if (!tags.TryGetValue(t.Key, out List<string> list))
                {
                    tags[t.Key] = list = new List<string>();
                }
                if (!list.Contains(t.Value.Item1))
                    list.Add(t.Value.Item1);
            }

            // Generate required tags
#pragma warning disable IDE0018 // Inline variable declaration
            List<string> req;
#pragma warning restore IDE0018 // Inline variable declaration
            if (tags.TryGetValue(Ao3TagType.Warnings, out req))
            {
                if (req.Count == 1 && idmap.TryGetValue(req[0], out int id) && TagIdToReqClass.TryGetValue(id, out string sclass))
                    model.RequiredTags[Ao3RequiredTag.Warnings] = new Ao3RequredTagData(sclass, req[0]);
                else
                    model.RequiredTags[Ao3RequiredTag.Warnings] = new Ao3RequredTagData("warning-yes", string.Join(", ", req));
            }
            if (tags.TryGetValue(Ao3TagType.Category, out req))
            {
                if (req.Count == 1 && idmap.TryGetValue(req[0], out int id) && TagIdToReqClass.TryGetValue(id, out string sclass))
                    model.RequiredTags[Ao3RequiredTag.Category] = new Ao3RequredTagData(sclass, req[0]);
                else
                    model.RequiredTags[Ao3RequiredTag.Category] = new Ao3RequredTagData("category-multi", string.Join(", ", req));
            }
            if (tags.TryGetValue(Ao3TagType.Rating, out req))
            {
                if (req.Count == 1 && idmap.TryGetValue(req[0], out int id) && TagIdToReqClass.TryGetValue(id, out string sclass))
                    model.RequiredTags[Ao3RequiredTag.Rating] = new Ao3RequredTagData(sclass, req[0]);
                else
                    model.RequiredTags[Ao3RequiredTag.Rating] = new Ao3RequredTagData("rating-na", string.Join(", ", req));
            }
        }

        static Dictionary<Ao3RequiredTag, Ao3TagType> RequiredTagToType = new Dictionary<Ao3RequiredTag, Ao3TagType> {
            {Ao3RequiredTag.Warnings, Ao3TagType.Warnings },
            {Ao3RequiredTag.Category, Ao3TagType.Category },
            {Ao3RequiredTag.Rating, Ao3TagType.Rating },
            {Ao3RequiredTag.Complete, Ao3TagType.Complete },
        };

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

        static Dictionary<string, string> SortColumnToName = new Dictionary<string, string>
        {
        };

        public class ChapterDetails
        {
            public string Name { get; set; }
            public long Id { get; set; }
            public long Number { get; set; }
        }


        public static async Task<List<ChapterDetails>> LookupChapters(Uri uri)
        {
            var model = LookupQuick(uri.AbsoluteUri);

            if (model?.Type != Ao3PageType.Work) return null;

            uri = new Uri(@"https://archiveofourown.org/works/" + model.Details.WorkId + "/navigate");
            var response = await HttpRequestAsync(uri);

            if (!response.IsSuccessStatusCode) return null;

            HtmlDocument doc = await response.Content.ReadAsHtmlDocumentAsync();

            var ol = doc?.GetElementbyId("main")?.ElementByClass("ol", "chapter");
            if (ol is null) return null;

            var chapters = new List<ChapterDetails>(ol.ChildNodes.Count);

            foreach (var li in ol.Elements("li"))
            {
                var a = li.Element("a");
                if (a is null) continue;

                var href = a.Attributes["href"];
                if (string.IsNullOrEmpty(href?.Value)) continue;

                var details = new ChapterDetails();
                
                var match = regexChapNumName.Match(a.InnerText.HtmlDecode());
                if (!match.Success) continue;

                if (!long.TryParse(match.Groups["CHAPTERNUM"].Value, out var num))
                    continue;

                details.Number = num;
                details.Name = match.Groups["CHAPTERNAME"].Value;

                match = regexWork.Match(new Uri(uri, href.Value.HtmlDecode()).LocalPath);
                if (!match.Success) continue;

                if (!long.TryParse(match.Groups["CHAPTERID"].Value, out num))
                    continue;

                details.Id = num;
                chapters.Add(details);
            }

            return chapters;

        }
    }
}
