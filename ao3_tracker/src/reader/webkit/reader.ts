namespace Ao3Track {

    // This is a mess. 

    export function GetWorkChapters(works: number[], callback: (workchapters: GetWorkChaptersMessageResponse) => void) {
        let hCallback = Ao3TrackCallbacks.Add(callback);
        Ao3TrackHelper.getWorkChaptersAsync(JSON.stringify(works),hCallback);
    }

    export function SetWorkChapters(workchapters: { [key: number]: IWorkChapter; }) {
        Ao3TrackHelper.setWorkChapters(JSON.stringify(workchapters));
    }

    export function SetNextPage(uri: string) {
        Ao3TrackHelper.set_NextPage(uri);
    }
    SetNextPage(jQuery('head link[rel=next]').attr('href') || "");

    export function SetPrevPage(uri: string) {
        Ao3TrackHelper.set_PrevPage(uri);
    }
    SetPrevPage(jQuery('head link[rel=prev]').attr('href') || "");

    export function DisableLastLocationJump() {
        Ao3TrackHelper.set_JumpToLastLocationEnabled(false);
        Ao3TrackHelper.set_JumpToLastLocationCallback(0);
    }

    export function EnableLastLocationJump(workid: number, lastloc: IWorkChapter) {
        let hCallback = Ao3TrackCallbacks.AddPermanent((ev:boolean) => { 
            if (Boolean(ev)) {
                GetWorkChapters([workid], (workchaps) => { 
                    lastloc = workchaps[workid];
                    Ao3Track.scrollToLocation(workid,lastloc,true); 
                });
            }
            else {
                Ao3Track.scrollToLocation(workid,lastloc,false); 
            }
        });
        Ao3TrackHelper.set_JumpToLastLocationCallback(hCallback);
        Ao3TrackHelper.set_JumpToLastLocationEnabled(true);
    }

    // Font size up/down support 
    function updatefontsize() {
        let inner = document.getElementById("inner");
        if (inner) {
            inner.style.fontSize = Ao3TrackHelper.get_FontSize().toString() + "%";
        }
    };
    Ao3TrackHelper.set_AlterFontSizeCallback(Ao3TrackCallbacks.AddPermanent(updatefontsize));
    updatefontsize();

    export function SetCurrentLocation(current : IWorkChapterEx)
    {
        Ao3TrackHelper.set_CurrentLocation(JSON.stringify(current));
    }

    function contextMenuHandler(ev: PointerEvent) {
        for (let target = ev.target as (HTMLElement|null); target && target !== document.body; target = target.parentElement) {
            let a = target as HTMLAnchorElement;
            if (target.tagName === "A" && a.href && a.href !== "") {
                ev.preventDefault();
                ev.stopPropagation();

                let hCallback = Ao3TrackCallbacks.Add((item:string) => {
                    switch (item)
                    {
                        case "Open":
                        {
                            window.location.href = a.href;
                        }
                        break;

                        case "Open and Add":
                        {
                            Ao3TrackHelper.addToReadingList(a.href);
                            window.location.href = a.href;
                        }
                        break;
                        
                        case "Add to Reading list":
                        {
                            Ao3TrackHelper.addToReadingList(a.href);
                        }
                        break;

                        case "Copy Link":
                        {
                            Ao3TrackHelper.copyToClipboard(a.href,"uri");
                        }
                        break;
                    }
                });

                Ao3TrackHelper.showContextMenu(ev.clientX, ev.clientY, JSON.stringify([
                    "Open", 
                    "Open and Add", 
                    "Add to Reading list", 
                    "Copy Link"
                ]), hCallback);
                
                return;
            }
        }
    }
    document.body.addEventListener("contextmenu", contextMenuHandler);
    Ao3TrackHelper.setCookies(document.cookie);
};
