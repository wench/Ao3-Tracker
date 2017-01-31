
namespace Ao3Track {


    export function GetWorkChapters(works: number[], callback: (workchapters: GetWorkChaptersMessageResponse) => void) {
        Ao3Track.Helper.getWorkChaptersAsync(works, callback);
    }

    export function SetWorkChapters(workchapters: { [key: number]: IWorkChapter; }) {
        Ao3Track.Helper.setWorkChapters(workchapters);
    }

    Ao3Track.Helper.nextPage = jQuery('head link[rel=next]').attr('href') || "";
    export function SetNextPage(uri: string) {
        Ao3Track.Helper.nextPage = uri;
    }

    Ao3Track.Helper.prevPage = jQuery('head link[rel=prev]').attr('href') || "";
    export function SetPrevPage(uri: string) {
        Ao3Track.Helper.prevPage = uri;
    }

    export function DisableLastLocationJump() {
        Ao3Track.Helper.jumpToLastLocationEnabled = false;
        Ao3Track.Helper.onjumptolastlocationevent = null;
    }

    export function EnableLastLocationJump(workid: number, lastloc: IWorkChapter) {
        Ao3Track.Helper.onjumptolastlocationevent = (ev) => {
            if (Boolean(ev)) {
                GetWorkChapters([workid], (workchaps) => { 
                    lastloc = workchaps[workid];
                    Ao3Track.scrollToLocation(workid,lastloc,true); 
                });
            }
            else {
                Ao3Track.scrollToLocation(workid,lastloc,false); 
            }
        }
        Ao3Track.Helper.jumpToLastLocationEnabled = true;
    }

    // Font size up/down support 
    let updatefontsize = () => {
        let inner = document.getElementById("inner");
        if (inner) {
            inner.style.fontSize = Ao3Track.Helper.fontSize.toString() + "%";
        }
    };
    Ao3Track.Helper.onalterfontsizeevent = updatefontsize;
    updatefontsize();

    export function SetCurrentLocation(current : IWorkChapterEx)
    {
        Ao3Track.Helper.currentLocation = current;
    }

    function contextMenuHandler(ev: PointerEvent) {
        for (let target = ev.target as (HTMLElement|null); target && target !== document.body; target = target.parentElement) {
            let a = target as HTMLAnchorElement;
            if (target.tagName === "A" && a.href && a.href !== "") {
                ev.preventDefault();
                ev.stopPropagation();

                Ao3Track.Helper.showContextMenu(ev.clientX, ev.clientY, [
                    "Open", 
                    "Open and Add", 
                    "Add to Reading list", 
                    "Copy Link"
                ], (item) => {
                    switch (item)
                    {
                        case "Open":
                        {
                            window.location.href = a.href;
                        }
                        break;

                        case "Open and Add":
                        {
                            Ao3Track.Helper.addToReadingList(a.href);
                            window.location.href = a.href;
                        }
                        break;
                        
                        case "Add to Reading list":
                        {
                            Ao3Track.Helper.addToReadingList(a.href);
                        }
                        break;

                        case "Copy Link":
                        {
                            Ao3Track.Helper.copyToClipboard(a.href,"uri");
                        }
                        break;
                    }
                });
                return;
            }
        }
    }
    document.body.addEventListener("contextmenu", contextMenuHandler);
    Ao3Track.Helper.setCookies(document.cookie);
};
