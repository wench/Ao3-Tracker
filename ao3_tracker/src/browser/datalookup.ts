// This is port of the Ao3SiteDataLookup code to javascript for primitive readinglist support
namespace Ao3Track {
    export namespace Data {

        let escTagStrings = ["*s*", "*a*", "*d*", "*q*", "*h*"];
        let usescTagStrings = ["/", "&", ".", "?", "#"];
        let regexEscTag = new RegExp("([/&.?#])", "g");
        let regexUnescTag = new RegExp("(\\*[sadqh])\\*", "g");
        let regexTag = new RegExp("^/tags/([^/?#]+)(?:/(works|bookmarks)?)?$");           // 1 => TAGNAME, 2 => TYPE
        let regexWork = new RegExp("^/works/(\\d+)(?:/chapters/(\\d+))?$");               // 1 => WORKID, 2 => CHAPTERID
        let regexWorkComment = new RegExp("^/works/(\\d+)/comments/(\\d+)$");             // 1 => WORKID, 2 => COMMENTID
        let regexRSSTagTitle = new RegExp("AO3 works tagged '(.*)'$");                    // 1 => TAGNAME
        let regexTagCategory = new RegExp("This tag belongs to the (\\w*) Category\\.");  // 1 => CATEGORY
        let regexPageQuery = new RegExp("(&?page=\\d+&?)");

        function EscapeTag(tag: string): string {
            return tag.replace(regexEscTag, (match) => {
                let i = usescTagStrings.indexOf(match);
                if (i !== -1) { return escTagStrings[i]; }
                return "";
            });
        }

        function UnescapeTag(tag: string): string {
            return tag.replace(regexUnescTag, (match) => {
                let i = escTagStrings.indexOf(match);
                if (i !== -1) { return usescTagStrings[i]; }
                return "";
            });
        }


        export function readingListlUri(url: string | URL): URL | null {
            if (typeof (url) === "string") {
                url = new URL(url);
            }
            url.search = url.search.replace(regexPageQuery, (m) => {
                if (m.startsWith("&") && m.endsWith("&")) { return "&"; }
                else { return ""; }
            });
            if (url.search === "?") {
                url.search = '';
            }

            if (url.hostname === "archiveofourown.org" || url.hostname === "www.archiveofourown.org") {
                url.protocol = "http";
                url.host = "archiveofourown.org";
            }
            else {
                return null;
            }

            url.hash = '';
            let match;
            if (match = url.pathname.match(regexWork))   // View Work
            {
                let sWORKID = match[1];
                url.pathname = "/works/" + sWORKID;
            }

            return url;
        }


        export function lookupAsync(urls: string[]): Promise<[string, (Ao3PageModel | null)][]> {
            let tasks: Promise<[string, Ao3PageModel | null]>[] = [];

            for (let url of urls) {
                tasks.push(new Promise<[string, Ao3PageModel | null]>((resolve, reject) => {
                    let uri = new URL(url);

                    uri.search = uri.search.replace(regexPageQuery, (m) => {
                        if (m.startsWith("&") && m.endsWith("&")) { return "&"; }
                        else { return ""; }
                    });
                    if (uri.search === "?") {
                        uri.search = '';
                    }

                    if (uri.hostname === "archiveofourown.org" || uri.hostname === "www.archiveofourown.org") {
                        uri.protocol = "http";
                        uri.host = "archiveofourown.org";
                    }
                    else {
                        resolve([url, null]);
                        return;
                    }
                    uri.hash = '';

                    let model = new Ao3PageModel(uri);

                    let match;

                    if (uri.pathname === "/works") // Work search and Advanced search
                    {
                        model.Type = Ao3PageType.Search;
                        model.Title = "Search";

                        FillModelFromSearchQuery(uri, model);
                    }
                    else if (uri.pathname === "/works/search") // Work search and Advanced search
                    {
                        model.Type = Ao3PageType.Search;
                        model.Title = "Advanced Search";

                        FillModelFromSearchQuery(uri, model);
                    }
                    else if (match = uri.pathname.match(regexTag))    // View tag
                    {
                        model.Type = Ao3PageType.Tag;

                        var sTAGNAME = match[1];
                        var sTYPE = match[2] || "";

                        model.Title = sTYPE.trim();
                        if (model.Title.length) {
                            model.Title = model.Title[0].toUpperCase() + model.Title.substr(1);
                        }

                        if (sTYPE === "works") {
                            model.Type = Ao3PageType.Search;
                            if (uri.search.indexOf("work_search") !== -1) {
                                model.Title = "Search";
                            }
                        }
                        else if (sTYPE === "bookmarks") {
                            model.Type = Ao3PageType.Bookmarks;
                        }
                        else {
                            model.Type = Ao3PageType.Tag;
                        }

                        /*
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
                        */
                        model.PrimaryTag = sTAGNAME;
                        model.PrimaryTagType = Ao3TagType.Other;
                        model.Tags = new Map<Ao3TagType, string[]>();

                        FillModelFromSearchQuery(uri, model);

                    }
                    else if (match = uri.pathname.match(regexWork))   // View Work
                    {
                        model.Type = Ao3PageType.Work;

                        var sWORKID = match[1];
                        model.Uri = uri = new URL("/works/" + sWORKID, uri.href);

                        model.Details = new Ao3WorkDetails();

                        model.Details.WorkId = parseInt(sWORKID);

                        var wsuri = new URL("http://archiveofourown.org/works/search?utf8=%E2%9C%93&work_search%5Bquery%5D=id%3A" + sWORKID);

                        new Promise<JQuery>((resolve) => {
                            let tryget = (retrying: boolean) => {
                                $.get({
                                    url: wsuri.href,
                                    dataType: "html",
                                    xhrFields: {
                                        withCredentials: retrying
                                    },
                                    error: (jqXHR: JQueryXHR, textStatus: string, errorThrown: string) => {
                                        if (!retrying) {
                                            tryget(true);
                                        }
                                        else {
                                            resolve($());
                                        }
                                    },
                                    success: (data, textStatus, jqXHR) => {
                                        let doc: Element[] = $.parseHTML(data, undefined, false);
                                        let worknode = $(doc).find("#work_" + sWORKID);
                                        if (worknode.length === 0) {
                                            tryget(true);
                                        }
                                        else {
                                            resolve(worknode);
                                        }
                                    }
                                });
                            }
                            tryget(false);
                        }).then((worknode) => {
                            if (worknode.length !== 0) { FillModelFromWorkSummary(wsuri, worknode, model); }
                            resolve([url, model]);
                        });
                        return;
                    }
                    resolve([url, model]);
                }));
            }

            return Promise.all(tasks);
        }

        function FillModelFromWorkSummary(baseuri: URL, worknode: JQuery, model: Ao3PageModel): void {
            let tags = model.Tags = new Map<Ao3TagType, string[]>();
            let tagsnode = worknode.children("ul.tags");

            if (!model.Details) { model.Details = new Ao3WorkDetails(); }

            debugger;
            tagsnode.children("li").each((index, elem) => {
                var tn = $(elem);
                var a = tn.find("a.tag").first();
                if (a.length === 0) {
                    return;
                }

                let type = Ao3TagType.Other;

                for (let e in Ao3TagType) {
                    if (typeof (e) !== "string" || !isNaN(parseInt(e))) { continue; }
                    if (tn.hasClass(e.toLowerCase())) {
                        type = Ao3TagType[e] as any;
                        break;
                    }
                }

                var href = a.attr("href");
                if (href) {
                    var reluri = new URL(href, baseuri.href);
                    var m = decodeURIComponent(reluri.pathname).match(regexTag);
                    if (m) {
                        var tag = m[1];

                        let list = tags.get(type);
                        if (list === undefined) {
                            tags.set(type, list = []);
                        }
                        list.push(UnescapeTag(tag));
                    }
                }
            });

            // Header
            let headernode = worknode.children("div.header");

            // Get Fandom tags
            let fandomnode = headernode.children(".fandoms");

            fandomnode.children("a").each((index, elem) => {
                var a = $(elem);
                var href = a.attr("href");
                if (href) {
                    var reluri = new URL(href, baseuri.href);
                    var m = decodeURIComponent(reluri.pathname).match(regexTag);
                    if (m) {
                        var tag = m[1];
                        let list = tags.get(Ao3TagType.Fandoms);
                        if (list === undefined) {
                            tags.set(Ao3TagType.Fandoms, list = []);
                        }
                        list.push(UnescapeTag(tag));

                    }
                }
            });

            let headingnode = headernode.children(".heading");
            if (headingnode.length)
            {
                let links = headingnode.children("a");
                let authors = new Map<string, string>();
                let recipiants = new Map<string, string>();
                if (links.length)
                {
                    let titlenode = links.first();
                    model.Title = titlenode.text();

                    links.each((index,elem)=>
                    {
                        let n = $(elem);
                        var href = n.attr("href");
                        var rel = n.attr("rel");
                        var uri = new URL(href,baseuri.href);

                        if (rel === "author")
                        {
                            authors.set(uri.href,n.text());
                        }
                        else if (href.endsWith("/gifts"))
                        {
                            recipiants.set(uri.href, n.text());
                        }
                    });
                }
                if (authors.size) { model.Details.Authors = authors; }
                if (recipiants.size) { model.Details.Recipiants = recipiants; }
            } 

            // Get requried tags
            let requirednode = headernode.children(".required-tags");
            let required = new Map<Ao3RequiredTag, JQuery>();
            required.set(Ao3RequiredTag.Rating, requirednode.find("span.rating"));
            required.set(Ao3RequiredTag.Warnings, requirednode.find("span.warnings"));
            required.set(Ao3RequiredTag.Category, requirednode.find("span.category"));
            required.set(Ao3RequiredTag.Complete,requirednode.find("span.iswip"));   

            model.RequiredTags = new Map<Ao3RequiredTag, [string, string]|null>();
            for (let n of required)
            {
                if (n[1].length === 0)
                {
                    model.RequiredTags.set(n[0], null);
                    continue;
                }

                var classes = n[1].attr("class").split(" ");
                var search = Ao3RequiredTag[n[0]].toLowerCase();
                var searchns = search;
                while (searchns.endsWith("s")) { searchns = searchns.substr(0,searchns.length-1); }
                search = search + "-";
                searchns = searchns + "-";
                var tag = classes.find((val) =>
                {
                    return val.startsWith(search) || val.startsWith(searchns);
                });

                if (tag) {
                    model.RequiredTags.set(n[0], [tag, n[1].text().trim()]);
                }
                else {
                    model.RequiredTags.set(n[0], null);
                }
            }      

            // Get primary tag... 
            let tagset : string[]|undefined;
            if ((tagset = tags.get(Ao3TagType.Relationships)) && tagset.length)
            {
                let tagname = tagset[0];
                //var tagdetails = await LookupTagAsync(tagname);
                //model.PrimaryTag = tagdetails.actual;
                model.PrimaryTag = tagname;
                model.PrimaryTagType = Ao3TagType.Relationships;
            }
            else if ((tagset = tags.get(Ao3TagType.Fandoms)) && tagset.length)
            {
                let tagname = tagset[0];
                //var tagdetails = await LookupTagAsync(tagname);
                //model.PrimaryTag = tagdetails.actual;
                model.PrimaryTag = tagname;
                model.PrimaryTagType = Ao3TagType.Fandoms;
            }

  
           // Stats

            let stats = worknode.children("dl.stats");

            model.Details.LastUpdated = headernode.children("p.datetime").text().trim();

            model.Language = stats.children("dd.language").text().trim();

            model.Details.Words = parseInt(stats.children("dd.words").text().trim().replace(",", ""));
            model.Details.Collections = parseInt(stats.children("dd.collections").text().trim());
            model.Details.Comments = parseInt(stats.children("dd.comments").text().trim());
            model.Details.Kudos = parseInt(stats.children("dd.kudos").text().trim());
            model.Details.Bookmarks = parseInt(stats.children("dd.bookmarks").text().trim());
            model.Details.Hits = parseInt(stats.children("dd.hits").text().trim());
            
            // Series

            let seriesnode = worknode.children("ul.series");
            if (seriesnode.length)
            {
                let series = new Map<string, [number, string]>();
                seriesnode.children("li").each((index,elem)=>
                {
                    let n = $(elem);
                    let link = n.children("a");
                    let linktext = link.text().trim();
                    if (linktext === "") {
                        return;
                    }

                    let s = link.attr("href");
                    if (s === "") {
                         return;
                    }
                    let uri = new URL(s, baseuri.href);

                    let part = n.children("strong").text();
                    if (part === "") {
                         return;
                    }

                    series.set(uri.href, [parseInt(part), linktext]);
                });
                if (series.size) {
                    model.Details.Series = series;
                }
            }

            let chapters = stats.children("dd.chapters").text().trim().split('/');
            if (chapters)
            {
                let total : number|null;
                if (chapters[1] === "?") { total = null; }
                else { total = parseInt(chapters[1]); }

                model.Details.Chapters = [null, parseInt(chapters[0]), total];
            }

            // Horrible horrible dirty grabbing of the summary
            let summarynode = worknode.children("blockquote.summary");
            model.Details.Summary = summarynode.html().trim();
        }

        function FillModelFromSearchQuery(uri: URL, model: Ao3PageModel): void {
            let query = new Map<string, string[]>();

            for (let v of uri.search.substr(1).split(/[;&]/))
            {
                var kv = v.split('=', 2);
                kv[0] = decodeURIComponent(kv[0]);

                let array = query.get(kv[0]);
                if (!array)
                {
                    query.set(kv[0], array = []);
                }
                if (kv.length === 2) {
                    array.push(decodeURIComponent(kv[1]));
                }
            }


            model.RequiredTags = new Map<Ao3RequiredTag, [string, string]>();
            var tlist : [Ao3TagType, [string, number]][] = [];

            for (var i in Ao3TagType)
            {
                if (typeof(i) !== "string" || !isNaN(parseInt(i))) {
                    continue;
                }
                
                var name = i.toLowerCase();
                while (name.endsWith('s')) { name = name.substr(0,name.length-1); }
                name = "work_search[" + name + "_ids][]";
                let tagids = query.get(name);

                if (tagids)
                {
                    for (var s of tagids)
                    {
                        let id = parseInt(s);

                        let tag = "<tag:"+id+">";//LookupTagQuick(id);
                        if (tag !== "") {
                            tlist.push([Ao3TagType[i] as any, [tag, id]]);
                        }
                    }
                }
            }

            let q;
            if (q = query.get("work_search[other_tag_names]"))
            {
                for (var tag of q[0].split(','))
                {
                    //var tagdetails = LookupTagQuick(tag);
                    //tlist.Add(new KeyValuePair<Ao3TagType, Tuple<string, int>>(GetTypeForCategory(tagdetails?.category), new Tuple<string, int>(UnescapeTag(tag), 0)));
                    tlist.push([Ao3TagType.Other as any, [tag, 0]]);
                }

            }


            if (q = query.get("work_search[language_id]"))
            {
                let id = parseInt(q[0]);
                if (!isNaN(id))
                {
                    //model.Language = LookupLanguageQuick(id);
                    model.Language = "<lang:"+id+">";
                }
                else if (q[0] === "")
                {
                    model.Language = "Any";
                }
            }
            else
            {
                model.Language = "Any";
            }


            if (q = query.get("work_search[complete]"))
            {
                let i = 0;
                let b = false;
                if (!isNaN(i = parseInt(q[0])) || q[0] === "false" || (b = (q[0]==="true")))
                {
                    if (i !== 0 || b)
                    {
                        model.RequiredTags.set(Ao3RequiredTag.Complete, ["complete-yes", "Complete only"]);
                    }
                    else
                    {
                    }
                }
            }
            if (!model.RequiredTags.has(Ao3RequiredTag.Complete) || model.RequiredTags.get(Ao3RequiredTag.Complete) === null) {
                model.RequiredTags.set(Ao3RequiredTag.Complete, ["category-none", "Complete and Incomplete"]);
            }

            let rq = model.RequiredTags.get(Ao3RequiredTag.Complete);
            if (rq) {
                tlist.push([Ao3TagType.Other, [rq[1], 0]]);
            }

            if (q = query.get("work_search[query]"))
            {
                model.SearchQuery = q[0];
            }

            if (q = query.get("tag_id"))
            {
                //var tagdetails = LookupTagQuick(query["tag_id"][0]);
                //model.PrimaryTag = tagdetails?.actual ?? query["tag_id"][0];
                //model.PrimaryTagType = GetTypeForCategory(tagdetails?.category);

                model.PrimaryTag = q[0];
                model.PrimaryTagType = Ao3TagType.Other;
            }

            // Now deal with tags that we looked up            
            let tags = model.Tags = model.Tags || new Map<Ao3TagType, string[]>();
            let idmap = new Map<string, number>();
            for (let t of tlist)
            {
                if (t[1][1] !== 0) { idmap.set(t[1][0], t[1][1]); }

                let list = tags.get(t[0]);
                if (!list)
                {
                    tags.set(t[0], list = []);
                }
                if (list.indexOf(t[1][0]) !== -1) {
                    list.push(t[1][0]);
                }
            }

            // Generate required tags
            let req;
            if (req = tags.get(Ao3TagType.Warnings))
            {
                let id;
                let sclass;
                if (req.length === 1 && (id = idmap.get(req[0])) && (sclass = TagIdToReqClass.get(id))) {
                    model.RequiredTags.set(Ao3RequiredTag.Warnings, [sclass, req[0]]);
                }
                else {
                    model.RequiredTags.set(Ao3RequiredTag.Warnings, ["warning-yes", req.join(", ")]);
                }
            }
            if (req = tags.get(Ao3TagType.Category))
            {
                let id;
                let sclass;
                if (req.length === 1 && (id = idmap.get(req[0])) && (sclass = TagIdToReqClass.get(id))) {
                    model.RequiredTags.set(Ao3RequiredTag.Category, [sclass, req[0]]);
                }
                else {
                    model.RequiredTags.set(Ao3RequiredTag.Category, ["category-multi", req.join(", ")]);
                }
            }
            if (req = tags.get(Ao3TagType.Rating))
            {
                let id;
                let sclass;
                if (req.length === 1 && (id = idmap.get(req[0])) && (sclass = TagIdToReqClass.get(id))) {
                    model.RequiredTags.set(Ao3RequiredTag.Rating, [sclass, req[0]]);
                }
                else {
                    model.RequiredTags.set(Ao3RequiredTag.Rating, ["rating-na", req.join(", ")]);
                }
            }            
        }

        let TagIdToReqClass = new Map<number, string>([
            [ 14, "warning-choosenotto" ],
            [ 16, "warning-no" ],
            [ 10, "rating-general-audience"],
            [ 11, "rating-teen"],
            [ 12, "rating-mature"],
            [ 13, "rating-explicit"],
            [ 9, "rating-notrated"],
            [ 116, "category-femslash"],
            [ 22, "category-het"],
            [ 21, "category-gen"],
            [ 23, "category-slash"],
            [ 2246, "category-multi"],
            [ 24, "category-other"],
        ]);        
    }
}
