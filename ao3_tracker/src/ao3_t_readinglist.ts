namespace Ao3Track {
    namespace ReadingList { 

        // For now, unlike the app, we don't store the reading list and don't even attempt to lookup the data for the works

        let readingListBacking : IReadingList = {
            last_sync: 0,
            paths: { }
        };

        function syncToServerAsync() {

            let req: ReadingListMessage = { type: "READING_LIST", data: {last_sync: readingListBacking.last_sync, paths: { } } };
            for(let uri in readingListBacking.paths) {
                let rle = readingListBacking.paths[uri];
                req.data.paths[uri] = rle.timestamp;
            }

            let callback = (response: ReadingListMessageResponse) => {
                if (response === null) {
                    return;
                }
                
                for(let uri in response.paths) {
                    let v = response.paths[uri];
                    if (v === -1) {
                        delete readingListBacking.paths[uri];
                    }
                    else {
                        if (readingListBacking.paths[uri] !== undefined) {
                            readingListBacking.paths[uri].timestamp = v;
                        }
                        else {
                            readingListBacking.paths[uri] = { uri: uri, timestamp:v};                
                        }
                    }
                }
                readingListBacking.last_sync = response.last_sync;
            };
            if (Ao3Track.processMessage) {
                Ao3Track.processMessage(req,null,callback);
            }
            else {
                chrome.runtime.sendMessage(req, callback);
            }
        }    

        //  Quick and dirty add to reading list
        export function add(href: string|URL) : void
        {
            let uri = Ao3Track.Data.readingListlUri(href);
            if (uri === null) {
                return;
            }
            if (Object.keys(readingListBacking.paths).indexOf(uri.href) === -1) {
                readingListBacking.paths[uri.href] = {uri: uri.href, timestamp:Date.now()};
                syncToServerAsync();                
            }
            Ao3Track.Data.lookupAsync([uri.href]).then((value)=>{
                debugger;
            });
        }

        // Context menus!
        if (chrome.contextMenus.create)
        {
            chrome.contextMenus.create({
                id: "Ao3Track-RL-Add",
                title: "Add To Reading List",
                contexts: ["link"],
                targetUrlPatterns: ["*://archiveofourown.org/*", "*://*.archiveofourown.org/*", "*://t.umblr.com/redirect?z=http%3A%2F%2Farchiveofourown.org%2F*"],
                onclick: (info, tab) => {
                    if (info.linkUrl) {
                        let url = new URL(info.linkUrl);
                        if (url.hostname === "t.umblr.com" && url.pathname === "/redirect") {
                            let search = url.search;
                            if (search[0] === '?') { search = search.substr(1); }
                            for (let comp of search.split(/[;&]/))
                            {
                                let kvp = comp.split("=", 2);
                                if (kvp[0] === "z") {
                                    add(decodeURIComponent(kvp[1]));
                                    return;
                                }
                            }
                        }
                        else {
                            add(url);
                        }
                    }
                }
            });
        }
    }
}

