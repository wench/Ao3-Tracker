namespace Ao3Track {

    sendMessage = (request: MessageRequest) => {
        chrome.runtime.sendMessage({type: request.type, data: request.data}, request.sendResponse);
    }

    GetWorkChapters = (works: number[], callback: (workchapters: { [key:number]:IWorkChapter }) => void) => {
        sendMessage({type: "GET", data: works, sendResponse: callback});
    };

    SetWorkChapters = (workchapters: { [key: number]: IWorkChapter; }) => {
        sendMessage({type: "SET", data: workchapters, sendResponse: undefined});
    };

    export function DoSync(callback: (result: boolean) => void) {
        sendMessage({type: "DO_SYNC", data: undefined, sendResponse: callback});
    };

    export function UserLogin(credentials: IUserLoginData, callback: (errors: FormErrorList) => void) {
        sendMessage({type: "USER_LOGIN", data: credentials, sendResponse: callback});
    };

    export function UserCreate(credentials: IUserCreateData, callback: (errors: FormErrorList) => void) {
        sendMessage({type: "USER_CREATE", data: credentials, sendResponse: callback});
    };

    export function UserLogout(callback: (result: boolean) => void) {
        sendMessage({type: "USER_LOGOUT",data: undefined, sendResponse: callback});
    };

    export function UserName(callback: (username: string) => void) {
        sendMessage({type: "USER_NAME", data: undefined, sendResponse: callback});
    };

    SetNextPage = (uri: string) => {
        let $e = jQuery('head link[rel=next]');
        if ($e.length > 0) {
            $e.attr('href', uri);
        }
        else {
            jQuery('<link rel="next"></link>').attr('href', uri).appendTo('head');
        }
    };
    SetPrevPage = (uri: string) => {
        let $e = jQuery('head link[rel=prev]');
        if ($e.length > 0) {
            $e.attr('href', uri);
        }
        else {
            jQuery('<link rel="prev"></link>').attr('href', uri).appendTo('head');
        }
    };

    let $actions = $('<div class=" actions" id="ao3t-actions"a></div>').appendTo("#outer");
    let $actions_ul = $('<ul></ul>').appendTo($actions);
    let $sync_now = $('<li><a href="#" id="ao3t-sync-now">Sync Now</a></li>').appendTo($actions_ul);

    $sync_now.click((eventObject) => {
        eventObject.preventDefault();
        eventObject.stopImmediatePropagation();
        DoSync((result) => {
        });
    });

    DisableLastLocationJump = () => {
        $('#ao3t-last-loc').remove();
    };

    let curLastLoc: IWorkChapter | null = null;
    EnableLastLocationJump = (workid: number, lastloc: IWorkChapter) => {
        if (curLastLoc === null) {
            $('<li><a href="#" id="ao3t-last-loc">Jump to previous</a></li>').appendTo($actions_ul).click((eventObject) => {
                eventObject.preventDefault();
                eventObject.stopImmediatePropagation();
                if (curLastLoc !== null) { scrollToLocation(workid, curLastLoc, true); }
            });
        }
        curLastLoc = lastloc;
    };

    SetCurrentLocation = (current : IWorkChapterEx) => {
    };

    AreUrlsInReadingList = (urls: string[], callback: (result: { [key:string]:boolean})=> void)  => {
        sendMessage({ type: "RL_ISINLIST", data: urls, sendResponse: callback });
    };
}
