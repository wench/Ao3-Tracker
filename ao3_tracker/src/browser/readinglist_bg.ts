namespace Ao3Track {
    namespace ReadingList { 

        let readingListBacking : IReadingList = {
            last_sync: 0,
            paths: { }
        };

        let readingListSynced = false;
        let readingListSyncing = false;
        let onreadinglistsync: Array<(success: boolean) => void> = [];
        
        function do_onreadinglistsync(success: boolean) {
            for (let i = 0; i < onreadinglistsync.length; i++) {
                onreadinglistsync[i](success);
            }
            onreadinglistsync = [];
        }

        function syncToServerAsync() : Promise<boolean> {            
            readingListSyncing = true;
            return new Promise<boolean> ((resolve) => {
                let srl : IServerReadingList = { last_sync: readingListBacking.last_sync, paths: { } };
                for(let uri in readingListBacking.paths) {
                    let rle = readingListBacking.paths[uri];
                    srl.paths[uri] = rle.timestamp;
                }

                Ao3Track.SyncReadingListAsync(srl).then((srl) => {
                    if (srl === null) {
                        readingListSyncing = false;
                        do_onreadinglistsync(false);
                        resolve(false);
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
                    readingListSynced = true;
                    readingListSyncing = false;
                    do_onreadinglistsync(true);
                    resolve(true);
                });
            });
        }    

        function syncIfNeededAsync() : Promise<boolean>
        {
            return new Promise<boolean> ((resolve) => {
                if (Ao3Track.syncingDisabled()) 
                {
                    resolve(readingListSynced);
                    return;
                }

                if (readingListSyncing) {
                    onreadinglistsync.push((result)=>{
                        resolve(result);
                        return;
                    });
                    return;
                }

                if (readingListSynced) {
                    resolve(true);
                    return;
                }

                syncToServerAsync().then((result) => {
                    resolve(result);
                    return;
                });
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
        }

        // Context menus!
        if (chrome.contextMenus.create)
        {
            chrome.contextMenus.create({
                id: "Ao3Track-RL-Add",
                title: "Add To Reading List",
                contexts: ["link"],
                targetUrlPatterns: ["*://archiveofourown.org/*", "*://*.archiveofourown.org/*"],
                onclick: (info, tab) => {
                    if (info.linkUrl) {
                        let url = new URL(info.linkUrl);
                        add(url);
                    }
                }
            });
        }

        Ao3Track.addMessageHandler((request: IsInReadingListMessage, sendResponse: (response: any) => void) => {
            switch(request.type)
            {
                case "RL_ISINLIST":
                {
                    syncIfNeededAsync().then((result) => {
                        let response : { [key: string]: boolean;  }= { };
                        for (let href of request.data)
                        {
                            let uri = Ao3Track.Data.readingListlUri(href);
                            if (uri !== null && Object.keys(readingListBacking.paths).indexOf(uri.href) !== -1) {
                                response[href] = true;
                            }            
                            else {
                                response[href] = false;
                            }                
                        }
                        sendResponse(response);
                    });
                }
                return true;
            }
            return false;
        });

        
    }
}

