namespace Ao3Track {
    namespace ReadingList { 

        let readingListBacking : IReadingList = {
            last_sync: 0,
            paths: { }
        };

        function syncToServerAsync() {

            let srl : IServerReadingList = { last_sync: readingListBacking.last_sync, paths: { } };
            for(let uri in readingListBacking.paths) {
                let rle = readingListBacking.paths[uri];
                srl.paths[uri] = rle.timestamp;
            }

            Ao3Track.SyncReadingListAsync(srl).then((srl) => {
                if (srl === null) {
                    return;
                }
                
                for(let uri in srl.paths) {
                    let v = srl.paths[uri];
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
                readingListBacking.last_sync = srl.last_sync;
            });
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

