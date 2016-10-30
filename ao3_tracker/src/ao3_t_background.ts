(function() {

// Convert a UTF16 string to UTF8 (expect trouble if the input isn't valid utf16)
function utf16_to_utf8(s: string): string {
    return unescape(encodeURIComponent(s));
}

// Convert a UTF8 string to UTF16 (expect trouble if the input isn't valid utf8)
function utf8_to_utf16(s: string): string {
    return decodeURIComponent(escape(s));
}

class WorkChapter implements IWorkChapterTS {
    number: number;
    chapterid: number;
    location: number | null;
    timestamp: number;

    constructor(num: number, chapid: number, loc: number | null, ts: number) {
        this.number = num;
        this.chapterid = chapid;
        this.location = loc;
        this.timestamp = ts;
    }

    static Create(other: IWorkChapterTS): WorkChapter {
        return new this(other.number, other.chapterid, other.location, other.timestamp);
    }

    IsNewer(newitem: IWorkChapter): boolean {

        if (newitem.number > this.number) { return true; }
        else if (newitem.number < this.number) { return false; }

        if (this.location === null) { return false; }
        if (newitem.location === null) { return true; }

        return newitem.location > this.location;
    }

    IsNewerOrSame(newitem: IWorkChapter): boolean {

        if (newitem.number > this.number) { return true; }
        else if (newitem.number < this.number) { return false; }

        if (newitem.location === null) { return true; }
        if (this.location === null) { return false; }

        return newitem.location >= this.location;
    }

}


let storage: { [key: number]: WorkChapter; } = {};
let unsynced: { [key: number]: WorkChapter; } = {};

enum SyncState {
    Disabled = -1,
    Syncing = 0,
    Ready = 1,
    Delayed = 2
}
let serversync: SyncState = SyncState.Syncing;
let last_sync = 0;
let no_sync_until = 0;
let timeout_id = 0;
let url_base = "https://wenchy.net/ao3track/api";
//let url_base = "http://localhost:56991/api";

let authorization = {
    username: "",
    credential: "",
    toBase64: function () {
        return window.btoa(utf16_to_utf8(this.username + '\n' + this.credential));
    }
};
/*
let messages = {
    list: [],
    add: function(msg) {}
};
*/


let onSyncFromServer: Array<(success: boolean) => void> = [];

function do_onSyncFromServer(success: boolean) {
    for (let i = 0; i < onSyncFromServer.length; i++) {
        onSyncFromServer[i](success);
    }
    onSyncFromServer = [];
}

function delayedsync(timeout: number): void {
    console.log("delayedsync: timeout = %i", timeout);
    let now = Date.now();
    if (timeout_id !== 0) {
        console.log("delayedsync: existing pending sync in %i", no_sync_until - now);
        // If the pending sync is going to happen before timeout would elapse, just let it happen
        if (no_sync_until <= now + timeout) { return; }
        clearTimeout(timeout_id);
        timeout_id = 0;
    }
    console.log("delayedsync: setting up timeout callback");
    no_sync_until = now + timeout;
    serversync = SyncState.Delayed;
    timeout_id = setTimeout(() => {
        console.log("delayedsync: timeout elapsed");
        clearTimeout(timeout_id);
        timeout_id = 0;
        dosync(true);
    }, timeout);
}

// dosync will fetch all values form the server newer than our last sync time, flush out everything in unsynced to the server, run all onSyncFromServer functions 
function dosync(force?: boolean) {
    console.log("dosync: starting sync. force = %s", force);

    if (authorization.username === null || authorization.username === "" || authorization.credential === null || authorization.credential === "") {
        serversync = SyncState.Disabled;
        do_onSyncFromServer(false);
        console.warn("dosync: FAILED. No credentials");
        return;
    }

    // Enforce 5 minutes gap between server sync. Don't want to hammer the server while scrolling through a fic  
    let now = Date.now();
    if (!force && now < no_sync_until && onSyncFromServer.length === 0) {
        console.log("dosync: have to wait %i for timeout", no_sync_until - now);
        delayedsync(no_sync_until - now);
        return;
    }

    if (timeout_id !== 0) {
        clearTimeout(timeout_id);
        timeout_id = 0;
    }
    no_sync_until = now + 5 * 60 * 1000;

    serversync = SyncState.Syncing; // set to syncing!

    // Attempt to sync from server!
    console.log("dosync: sending GET request");
    jQuery.ajax({
        url: url_base + "/Values?after=" + last_sync,
        crossDomain: true,
        type: "GET",
        headers: { 'Authorization': "Ao3track " + authorization.toBase64() },
        dataType: "json",
        error: function (jqXHR: JQueryXHR, textStatus: string, errorThrown: string) {
            console.error("dosync: FAILED %s", textStatus);
            serversync = SyncState.Disabled;
            do_onSyncFromServer(false);
        },
        success: function (items: { [key: number]: IWorkChapterTS; }, textStatus: string, jqXHR: JQueryXHR) {
            console.log("dosync: SUCCESS. %i items", Object.keys(items).length);

            let newitems: { [key: number]: IWorkChapterTS; } = {};
            for (let key in items) {
                // Highest time value of incoming item is our new sync time
                if (items[key].timestamp > last_sync) { last_sync = items[key].timestamp; }

                if (!(key in storage) || storage[key].IsNewerOrSame(items[key])) {
                    // Remove from unsynced list (if it exists)
                    if (key in unsynced) { delete unsynced[key]; }
                    // Grab the new details
                    newitems[key] = storage[key] = WorkChapter.Create(items[key]);
                }
                // This kinda shouldn't happen, but apparently it did... we can deal with it though
                else {
                    // Update the timestamp to newer than newest
                    if (storage[key].timestamp <= items[key].timestamp) { storage[key].timestamp = items[key].timestamp + 1; }
                    else { items[key].timestamp += 1; }
                    // set as unsynced
                    unsynced[key] = storage[key];
                }
            }

            // Write back the new values to local storage!
            chrome.storage.local.set(newitems);

            do_onSyncFromServer(true);

            // Write back to server if needed
            console.log("dosync: %i unsynced items to POST back", Object.keys(unsynced).length);
            if (Object.keys(unsynced).length > 0) {
                let current = unsynced;
                unsynced = {};
                let time = last_sync;
                for (let key in current) {
                    if (current[key].timestamp > time) {
                        time = current[key].timestamp;
                    }
                }
                serversync = SyncState.Syncing;
                console.log("dosync: sending POST request");
                jQuery.ajax({
                    url: url_base + "/Values",
                    crossDomain: true,
                    type: "POST",
                    headers: { 'Authorization': "Ao3track " + authorization.toBase64() },
                    dataType: "json",
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify(current),
                    error: function (jqXHR: JQueryXHR, textStatus: string, errorThrown: string) {
                        console.error("dosync: FAILED %s", textStatus);
                        serversync = SyncState.Disabled;
                        chrome.storage.local.set({ 'last_sync': 0 });
                        last_sync = 0;
                        do_onSyncFromServer(false);
                    },
                    success: function (items: { [key: number]: IWorkChapterTS; }, textStatu: string, jqXHR: JQueryXHR) {
                        console.log("dosync: SUCCESS. %i conflicted items", Object.keys(items).length);
                        if (Object.keys(items).length > 0) {
                            chrome.storage.local.set({ 'last_sync': 0 });
                            last_sync = 0;
                            dosync(true);
                            return;
                        }
                        if (time > last_sync) {
                            last_sync = time;
                            chrome.storage.local.set({ 'last_sync': time });
                        }

                        if (Object.keys(unsynced).length > 0) { dosync(true); }
                        else {
                            serversync = SyncState.Ready;
                            do_onSyncFromServer(true);
                        }
                    }
                });
            } else {
                chrome.storage.local.set({ 'last_sync': last_sync });
                serversync = SyncState.Ready;
            }
        }
    });
}

chrome.storage.local.get(function (items) {
    if (typeof chrome.runtime.lastError !== "undefined") {
        return;
    }
    if ('last_sync' in items) {
        last_sync = items['last_sync'];
        delete items['last_sync'];
    }
    if ('username' in items) {
        authorization.username = items['username'];
        chrome.storage.local.remove('username');
        delete items['username'];
    }
    if ('password' in items) {
        authorization.credential = items['password'];
        chrome.storage.local.remove('password');
        delete items['password'];
    }
    if ('authorization' in items) {
        authorization.username = items['authorization'].username;
        authorization.credential = items['authorization'].credential;
        delete items['authorization'];
    }
    for (let key in items) {
        try {
            let k = parseInt(key);
            let loc: number | null = null;
            if (typeof items[k].location === "undefined") {
                loc = null;
            } else if (items[k].location !== null) {
                loc = parseInt(items[k].location);
            }

            storage[k] = new WorkChapter(
                parseInt(items[key].number),
                parseInt(items[key].chapterid),
                loc,
                parseInt(items[key].timestamp)
            );

            if (storage[k].timestamp > last_sync) {
                unsynced[k] = storage[k];
            }
        } catch (e) {
        }
    }
    dosync();

    window.setInterval(dosync, 1000 * 60 * 60 * 6);
});


chrome.runtime.onMessage.addListener(function (request: MessageType, sender: chrome.runtime.MessageSender, sendResponse: (response: any) => void) {
    switch (request.type) {
        case 'GET':
            {
                let getResponse = function () {
                    let r: { [key: number]: IWorkChapter; } = {};
                    for (let id of request.data) {
                        if (id in storage) {
                            r[id] = storage[id];
                        }
                    }
                    return r;
                };
                if (serversync === SyncState.Syncing) {
                    onSyncFromServer.push(function () { sendResponse(getResponse()); });
                    return true;
                }
                sendResponse(getResponse());
            }
            return false;

        case 'SET':
            {
                let time = Date.now();
                let newitems: { [key: number]: IWorkChapterTS; } = {};
                let do_delayed = false;
                for (let id in request.data) {
                    if (!(id in storage) || storage[id].IsNewer(request.data[id])) {
                        // Do a delayed since if we finished a chapter, or started a new one 
                        if (request.data[id].location === null || request.data[id].location === 0 || (id in storage && request.data[id].chapterid !== storage[id].chapterid)) {
                            do_delayed = true;
                        }
                        newitems[id] = storage[id] = new WorkChapter(
                            request.data[id].number,
                            request.data[id].chapterid,
                            request.data[id].location,
                            time
                        );

                        unsynced[id] = storage[id];
                    }
                }
                if (Object.keys(newitems).length) {
                    chrome.storage.local.set(newitems);
                    if (serversync === SyncState.Ready || serversync === SyncState.Delayed) {
                        if (do_delayed) {
                            delayedsync(20 * 1000);
                        }
                        else {
                            dosync();
                        }
                    }
                }
            }
            return false;

        case 'DO_SYNC':
            {
                if (serversync === SyncState.Disabled) {
                    sendResponse(false);
                    return false;
                }
                else {
                    onSyncFromServer.push(function () { sendResponse(true); });
                    if (serversync !== SyncState.Syncing) { dosync(true); }
                }
            }
            return true;

        case 'USER_CREATE':
            {
                serversync = SyncState.Disabled;
                authorization.username = "";
                authorization.credential = "";
                chrome.storage.local.remove('authorization');

                jQuery.ajax({
                    url: url_base + "/User/Create",
                    crossDomain: true,
                    type: "POST",
                    dataType: "json",
                    data: { "username": request.data.username, "password": request.data.password, "email": request.data.email },
                    error: function (jqXHR, textStatus, errorThrown) {
                        sendResponse(null);
                    },
                    success: function (response, textStatus, jqXHR) {
                        if (typeof response === "string") {
                            authorization.username = request.data.username;
                            authorization.credential = response;
                            last_sync = 0; // force a full resync
                            chrome.storage.local.set({ 'authorization': authorization, 'last_sync': 0 });
                            serversync = SyncState.Syncing;
                            dosync(true);
                            sendResponse({});
                        } else if (typeof response === "object" && Object.keys(response).length > 0) {
                            sendResponse(response);
                        } else {
                            sendResponse(null);
                        }
                    }
                });
            }
            return true;

        case 'USER_LOGIN':
            {
                serversync = SyncState.Disabled;
                authorization.username = "";
                authorization.credential = "";
                chrome.storage.local.remove('authorization');

                jQuery.ajax({
                    url: url_base + "/User/Login",
                    crossDomain: true,
                    type: "POST",
                    dataType: "json",
                    data: { "username": request.data.username, "password": request.data.password },
                    error: function (jqXHR, textStatus, errorThrown) {
                        sendResponse(null);
                    },
                    success: function (response, textStatus, jqXHR) {
                        if (typeof response === "string") {
                            authorization.username = request.data.username;
                            authorization.credential = response;
                            last_sync = 0; // force a full resync
                            chrome.storage.local.set({ 'authorization': authorization, 'last_sync': 0 });
                            serversync = SyncState.Syncing;
                            dosync(true);
                            sendResponse({});
                        } else if (typeof response === "object" && Object.keys(response).length > 0) {
                            sendResponse(response);
                        } else {
                            sendResponse(null);
                        }
                    }
                });
            }
            return true;
    };
    return false;
});

})();
