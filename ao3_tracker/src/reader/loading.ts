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

interface CaretPosition {
    offsetNode: Node;
    offset: number;
}

interface Document {
    caretPositionFromPoint(x: number, y: number): CaretPosition;
}

namespace Ao3Track {
    GetWorkDetails = (works: number[], callback: (details: { [key: number]: IWorkDetails }) => void, flags?: WorkDetailsFlags) => {
        Ao3Track.Helper.getWorkDetailsAsync(works, flags || WorkDetailsFlags.All, callback);
    };

    SetWorkChapters = (workchapters: { [key: number]: IWorkChapter; }) => {
        Ao3Track.Helper.setWorkChapters(workchapters);
    };

    ShouldFilterWork = (workid: number, authors: string[], tags: string[], series: number[], callback: (filter: string | null) => void) => {
        Ao3Track.Helper.shouldFilterWork(workid, authors || [], tags || [], series || [], callback);
    };

    DisableLastLocationJump = () => {
        Ao3Track.Helper.onjumptolastlocationevent = null;
    };

    EnableLastLocationJump = (workid: number, lastloc: IWorkChapter) => {
        Ao3Track.Helper.onjumptolastlocationevent = (ev) => {
            if (Boolean(ev)) {
                Ao3Track.Helper.getWorkDetailsAsync([workid], WorkDetailsFlags.SavedLoc, (details) => {
                    let d = details[workid] || {};
                    if (d.savedLoc) {
                        lastloc = d.savedLoc;
                        Ao3Track.scrollToLocation(workid, lastloc, true);
                    }
                });
            }
            else {
                Ao3Track.scrollToLocation(workid, lastloc, false);
            }
        };
    };

    // Font size up/down support 
    Ao3Track.Helper.onalterfontsizeevent = (ev) => {
        Ao3Track.CSS.SetFontSize(Number(ev));
    };

    SetCurrentLocation = (current: IWorkChapterEx) => {
        Ao3Track.Helper.currentLocation = current;
    };

    AreUrlsInReadingList = (urls: string[], callback: (result: { [key: string]: boolean }) => void) => {
        Ao3Track.Helper.areUrlsInReadingListAsync(urls, callback);
    };

    Settings = Ao3Track.Helper.settings;

    export function contextMenuForAnchor(evtarget : EventTarget, clientX: number, clientY: number)
    {
        let clientToDev = Ao3Track.Helper.deviceWidth / window.innerWidth;
        for (let target = evtarget as (HTMLElement | null); target && target !== document.body; target = target.parentElement) {
            let a = target as HTMLAnchorElement;
            if (target.tagName === "A" && a.href && a.href !== "") {
                Ao3Track.Helper.showContextMenu(clientX * clientToDev, clientY * clientToDev, a.href, a.innerText);
                return true;
            }
        }
    }

    function contextMenuHandler(ev: MouseEvent) {
        console.log(ev);
        if (contextMenuForAnchor(ev.target, ev.clientX, ev.clientY))
        {
            ev.preventDefault();
            ev.stopPropagation();
            return;
        }

        let clientToDev = Ao3Track.Helper.deviceWidth / window.innerWidth;

        let selection = window.getSelection();
        for (let i = 0; i < selection.rangeCount; i++) {
            let range = selection.getRangeAt(i);
            let rect = range.getBoundingClientRect();
            if (rect.top <= ev.clientY && rect.bottom >= ev.clientY && rect.left <= ev.clientX && rect.right >= ev.clientX) {
                let rects = range.getClientRects();
                for (let j = 0; j < rects.length; j++) {
                    rect = rects.item(j);
                    if (rect.top <= ev.clientY && rect.bottom >= ev.clientY && rect.left <= ev.clientX && rect.right >= ev.clientX) {
                        let str = selection.toString();
                        ev.preventDefault();
                        ev.stopPropagation();
                        Ao3Track.Helper.showContextMenu(ev.clientX * clientToDev, ev.clientY * clientToDev, "", str);
                        return;
                    }
                }
            }
        }

        let node: Node | undefined;
        let offset: number = 0;

        if (document.caretPositionFromPoint) {
            let range = document.caretPositionFromPoint(ev.pageX, ev.pageY);
            node = range.offsetNode;
            offset = range.offset;
        }
        /*else if (document.caretRangeFromPoint) {
            let range = document.caretRangeFromPoint(ev.pageX, ev.pageY);
            node = range.startContainer;
            offset = range.startOffset;
        }*/
        else if (document.createRange) {
            let elem = ev.target as Node;
            for (let i = 0; i < elem.childNodes.length; i++) {
                if (elem.childNodes[i].nodeType !== 3) continue;

                let currentPos = 0;
                let endPos = (elem.childNodes[i] as Text).data.length - 1;
                for (let currentPos = 0; currentPos < endPos; currentPos++) {
                    let range = document.createRange();
                    range.setStart(elem.childNodes[i], currentPos);
                    range.setEnd(elem.childNodes[i], currentPos + 1);

                    let rects = range.cloneRange().getClientRects();
                    for (let j = 0; j < rects.length; j++) {
                        let rect = rects.item(j);
                        if (rect.top <= ev.clientY && rect.bottom >= ev.clientY && rect.left <= ev.clientX && rect.right >= ev.clientX) {
                            offset = currentPos;
                            node = elem.childNodes[i];
                            break;
                        }
                    }
                    range.detach();
                    if (node) break;
                }
                if (node) break;
            }
        }

        if (!node) return;

        // only split TEXT_NODEs
        if (node.nodeType === 3) {
            // Get the word at the position
            let textNode = node as Text;
            let split = textNode.data.split(/\b/);
            let c = 0;
            for (let i = 0; i < split.length; i++) {
                if (c <= offset && c + split[i].length >= offset) {
                    ev.preventDefault();
                    ev.stopPropagation();
                    Ao3Track.Helper.showContextMenu(ev.clientX * clientToDev, ev.clientY * clientToDev, "", split[i].trim());
                    return;
                }
                c += split[i].length;
            }
        }


    }
    addEventListener.window("contextmenu", contextMenuHandler);

    export function contextMenuForSelection()
    {
        let clientToDev = Ao3Track.Helper.deviceWidth / window.innerWidth;
        let selection = window.getSelection();
        for(let r = 0; r < selection.rangeCount; r++)
        {
            let range = selection.getRangeAt(r);
            let rects = range.getClientRects();

            for (let cr = 0; cr < rects.length; cr++)
            {
                let rect = rects.item(cr);
                var str = selection.toString();
                Ao3Track.Helper.showContextMenu(rect.left * clientToDev, rect.top * clientToDev, "", str);
                return true;
            }
        }
        return false;
    }

    Ao3Track.Helper.setCookies(document.cookie);
}