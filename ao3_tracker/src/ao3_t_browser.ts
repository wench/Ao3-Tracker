/// <reference path="../typings/globals/chrome/index.d.ts" />

namespace Ao3Track {
    export function GetWorkChapters(works: number[], callback: (workchapters: GetWorkChaptersMessageResponse) => void) {
        let msg: GetWorkChaptersMessage = { type: "GET", data: works };
        chrome.runtime.sendMessage(msg, callback);
    }

    export function SetWorkChapters(workchapters: { [key: number]: IWorkChapter; }) {
        let msg: SetWorkChaptersMessage = { type: "SET", data: workchapters };
        chrome.runtime.sendMessage(msg);
    }

    export function DoSync(callback: (result: boolean) => void) {
        let msg: DoSyncMessage = { type: "DO_SYNC" };
        chrome.runtime.sendMessage(msg, callback);
    }

    export function UserLogin(credentials: IUserLoginData, callback: (errors: FormErrorList) => void) {
        let req: UserLoginMessage = { type: "USER_LOGIN", data: credentials };
        chrome.runtime.sendMessage(req, callback);
    }

    export function UserCreate(credentials: IUserCreateData, callback: (errors: FormErrorList) => void) {
        let req: UserCreateMessage = { type: "USER_CREATE", data: credentials };
        chrome.runtime.sendMessage(req, callback);
    }

    export function SetNextPage(uri : string) {
        let $e =  jQuery('head link[rel=next]');
        if ($e.length > 0) {
            $e.attr('href', uri);
        }
        else {
            jQuery('<link rel="next"></link>').attr('href', uri).appendTo('head');
        }
    }
    export function SetPrevPage(uri : string) {
        let $e =  jQuery('head link[rel=prev]');
        if ($e.length > 0) {
            $e.attr('href', uri);
        }
        else {
            jQuery('<link rel="prev"></link>').attr('href', uri).appendTo('head');
        }
    }
    
    let $actions = $('<div class=" actions" id="ao3t-actions"a></div>').appendTo("#outer");
    let $actions_ul = $('<ul></ul>').appendTo($actions);
    let $sync_now = $('<li><a href="#" id="ao3t-sync-now">Sync Now</a></li>').appendTo($actions_ul);

    $sync_now.click((eventObject) => {
        eventObject.preventDefault();
        eventObject.stopImmediatePropagation();
        DoSync((result) => {
        });
    });

    export function DisableLastLocationJump()    {
        $('#ao3t-last-loc').remove();
    }

    export function EnableLastLocationJump(lastloc: IWorkChapter)    {
        $('<li><a href="#" id="ao3t-last-loc">Jump to previous</a></li>').appendTo($actions_ul).click((eventObject) => {
            eventObject.preventDefault();
            eventObject.stopImmediatePropagation();
            scrollToLocation(lastloc);
        });
    }
}
