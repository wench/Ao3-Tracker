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

namespace Ao3Track {

    sendMessage = (request: MessageRequest) => {
        chrome.runtime.sendMessage({ type: request.type, data: request.data }, request.sendResponse);
    };

    GetWorkDetails = (works: number[], callback: (details: { [key: number]: IWorkDetails }) => void, flags?: WorkDetailsFlags) => {
        if (!flags) flags = WorkDetailsFlags.All;
        let finished: WorkDetailsFlags = 0;
        let result: { [key: number]: IWorkDetails } = {};

        if (flags & WorkDetailsFlags.SavedLoc) {
            sendMessage({
                type: "GET", data: works, sendResponse: (reponse: { [key: number]: IWorkChapter }) => {
                    for (let workid in reponse) {
                        let detail = result[workid] || {};
                        detail.savedLoc = reponse[workid];
                    }
                    finished = finished | WorkDetailsFlags.SavedLoc;
                    if (finished === flags) callback(result);
                }
            });
        }

        if (flags & WorkDetailsFlags.InReadingList) {
            sendMessage({
                type: "RL_WORKINLIST", data: works, sendResponse: (reponse: { [key: number]: boolean; }) => {
                    for (let workid in reponse) {
                        let detail = result[workid] || {};
                        detail.inReadingList = reponse[workid];
                    }
                    finished = finished | WorkDetailsFlags.InReadingList;
                    if (finished === flags) callback(result);
                }
            });
        }
    };

    SetWorkChapters = (workchapters: { [key: number]: IWorkChapter; }) => {
        sendMessage({ type: "SET", data: workchapters, sendResponse: undefined });
    };

    ShouldFilterWork = (workid: number, authors: string[], tags: string[], series: number[], callback: (filter: string | null) => void) => {
        callback(null);
    };

    export function DoSync(callback: (result: boolean) => void) {
        sendMessage({ type: "DO_SYNC", data: undefined, sendResponse: callback });
    };

    export function UserLogin(credentials: IUserLoginData, callback: (errors: FormErrorList) => void) {
        sendMessage({ type: "USER_LOGIN", data: credentials, sendResponse: callback });
    };

    export function UserCreate(credentials: IUserCreateData, callback: (errors: FormErrorList) => void) {
        sendMessage({ type: "USER_CREATE", data: credentials, sendResponse: callback });
    };

    export function UserLogout(callback: (result: boolean) => void) {
        sendMessage({ type: "USER_LOGOUT", data: undefined, sendResponse: callback });
    };

    export function UserName(callback: (username: string) => void) {
        sendMessage({ type: "USER_NAME", data: undefined, sendResponse: callback });
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

    SetCurrentLocation = (current: IWorkChapterEx) => {
    };

    AreUrlsInReadingList = (urls: string[], callback: (result: { [key: string]: boolean }) => void) => {
        sendMessage({ type: "RL_URLSINLIST", data: urls, sendResponse: callback });
    };

    Settings = {
        tempToC: true,
        distToM: true,
        volumeToM: true,
        weightToM: true,
        showCatTags: true,
        showRatingTags: true,
        showWIPTags: true,
        hideFilteredWorks: false
    };
}
