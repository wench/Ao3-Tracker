/// <reference path="jsinterop.d.ts" />

namespace Ao3Track {

    // This is a mess. Need to manually marshal between { [key: number]: IWorkChapter } and IDictionary<long,WorkChapter>

    function ToAssocArray<V>(map: Ao3TrackHelper.IIterable<Ao3TrackHelper.IKeyValuePair<number, V>>): { [key: number]: V } {
        var response: { [key: number]: V } = {};
        for (var it = map.first(); it.hasCurrent; it.moveNext()) {
            var i = it.current;
            response[i.key] = i.value;
        }
        return response;
    }

    export function GetWorkChapters(works: number[], callback: (workchapters: GetWorkChaptersMessageResponse) => void) {
        Ao3TrackHelper.getWorkChaptersAsync(works).then((result) => {
            callback(ToAssocArray<IWorkChapter>(result));
        });
    }

    export function SetWorkChapters(workchapters: { [key: number]: IWorkChapter; }) {
        var m = Ao3TrackHelper.createWorkChapterMap();
        for (let key in workchapters) {
            m.insert(key as any, Ao3TrackHelper.createWorkChapter(workchapters[key].number, workchapters[key].chapterid, workchapters[key].location, workchapters[key].seq));
        }
        Ao3TrackHelper.setWorkChapters(m);
    }

    Ao3TrackHelper.nextPage = jQuery('head link[rel=next]').attr('href') || "";
    export function SetNextPage(uri: string) {
        Ao3TrackHelper.nextPage = uri;
    }

    Ao3TrackHelper.prevPage = jQuery('head link[rel=prev]').attr('href') || "";
    export function SetPrevPage(uri: string) {
        Ao3TrackHelper.prevPage = uri;
    }

    export function DisableLastLocationJump() {
        Ao3TrackHelper.jumpToLastLocationEnabled = false;
        Ao3TrackHelper.onjumptolastlocationevent = null;
    }

    export function EnableLastLocationJump(workid: number, lastloc: IWorkChapter) {
        Ao3TrackHelper.onjumptolastlocationevent = (ev) => {
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
        Ao3TrackHelper.jumpToLastLocationEnabled = true;
    }

    // Font size up/down support 
    let updatefontsize = () => {
        let inner = document.getElementById("inner");
        if (inner) {
            inner.style.fontSize = Ao3TrackHelper.fontSize.toString() + "%";
        }
    };
    Ao3TrackHelper.onalterfontsizeevent = updatefontsize;
    updatefontsize();

    export function SetCurrentLocation(current : IWorkChapter)
    {
        Ao3TrackHelper.currentLocation = Ao3TrackHelper.createWorkChapter(current.number,current.chapterid,current.location,current.seq);
    }

    export function SetCurrentWorkId(current : number)
    {
        Ao3TrackHelper.currentWorkId = current;
    }

    // Nonsense to allow for swiping back and foward between pages 

    let dragging = false;
    let canforward = false;
    let canbackward = false;
    let startTouchX: number = 0;
    let startTouchY: number = 0;
    const startLimit = window.innerWidth;
    const endThreshold = window.innerWidth / 6;
    const maxSlide = window.innerWidth;
    const minThreshold = endThreshold / 4;
    const yLimit = window.innerHeight / 8;
    let zoomFactor = window.screen.availWidth * window.screen.deviceXDPI / (window.screen.logicalXDPI * window.innerWidth);
    interface TouchEventSubset {
        touches: {
            length: number,
            item: (index: number) => { screenX: number, screenY: number }
        };
    }
    function pointerDownHandler(event: PointerEvent) {
        // Only Touch
        if (event.pointerType !== "touch") {
            removeTouchEvents();
            return;
        }

        // Only primary
        if (!event.isPrimary) {
            removeTouchEvents();
            return;
        }

        let te: TouchEventSubset = {
            touches: {
                length: 1,
                item: (index: number) => { return event; }
            }
        };

        touchStartHandler(te as TouchEvent);

        window.addEventListener("pointermove", pointerMoveHandler);
        window.addEventListener("pointerup", pointerEndHandler);
    }
    function touchStartHandler(event: TouchEvent) {
        let touch = event.touches.item(0);
        dragging = false;
        if (event.touches.length > 1 || !touch) {
            removeTouchEvents();
            return;
        }
        startTouchX = touch.screenX / zoomFactor;
        startTouchY = touch.screenY / zoomFactor;

        canforward = false;
        canbackward = false;
        if (Ao3TrackHelper.canGoBack && startTouchX < startLimit) {
            // going backwards....
            canbackward = true;
        }
        if (Ao3TrackHelper.canGoForward && startTouchX >= (window.innerWidth - startLimit)) {
            // Going forwards
            canforward = true;
        }
        if (!canbackward && !canforward) {
            removeTouchEvents();
            return;
        }
        dragging = true;
        if ('ontouchmove' in document) { document.addEventListener("touchmove", touchMoveHandler); }
        if ('ontouchend' in document) { document.addEventListener("touchend", touchEndHandler); }
        if ('ontouchcancel' in document) { document.addEventListener("touchcancel", touchCancelHandler); }
    }
    let lastTouchX: number = 0;
    let lastTouchY: number = 0;
    function pointerMoveHandler(event: PointerEvent) {
        // Only Touch
        if (event.pointerType !== "touch") {
            removeTouchEvents();
            return;
        }

        // Only primary
        if (!event.isPrimary) {
            removeTouchEvents();
            return;
        }

        let te: TouchEventSubset = {
            touches: {
                length: 1,
                item: (index: number) => { return event; }
            }
        };

        touchMoveHandler(te as TouchEvent);
    }
    function touchMoveHandler(event: TouchEvent) {
        let touch = event.touches.item(0);
        if (!dragging || event.touches.length > 1 || !touch) {
            removeTouchEvents();
            return;
        }
        lastTouchX = touch.screenX / zoomFactor;
        lastTouchY = touch.screenY / zoomFactor;

        let offset = lastTouchX - startTouchX;
        let offsetY = Math.abs(lastTouchY - startTouchY);

        // Too much y movement? Disable this entirely 
        if (offsetY >= yLimit * 2) {
            removeTouchEvents();
            return;
        }

        if ((!canbackward && offset > 0.0) || (!canforward && offset < 0.0) || (offset > 0.0 && offset < minThreshold) || (offset < 0.0 && offset > -minThreshold) ||
            (offsetY >= yLimit)) {
            offset = 0.0;
        }
        else if (offset < -maxSlide) {
            offset = -maxSlide;
        }
        else if (offset > maxSlide) {
            offset = maxSlide;
        }

        // css class handling
        if (canforward && offset < -endThreshold && offsetY < yLimit) {
            Ao3TrackHelper.showNextPageIndicator = true;
        }
        else {
            Ao3TrackHelper.showNextPageIndicator = false;
        }

        if (canbackward && offset >= endThreshold && offsetY < yLimit) {
            Ao3TrackHelper.showPrevPageIndicator = true;
        }
        else {
            Ao3TrackHelper.showPrevPageIndicator = false;
        }

        Ao3TrackHelper.leftOffset = offset;
        //Ao3TrackHelper.opacity = (window.innerWidth - Math.abs(offset)) / window.innerWidth;
    }
    function pointerEndHandler(event: PointerEvent) {
        // Only Touch
        if (event.pointerType !== "touch") {
            removeTouchEvents();
            return;
        }

        // Only primary
        if (!event.isPrimary) {
            removeTouchEvents();
            return;
        }

        let te: TouchEventSubset = {
            touches: {
                length: 1,
                item: (index: number) => { return event; }
            }
        };

        touchEndHandler(te as TouchEvent);
    }
    function touchEndHandler(event: TouchEvent) {
        if (!dragging) { 
            removeTouchEvents();
            return; 
        }
        let offset = lastTouchX - startTouchX;
        let offsetY = Math.abs(lastTouchY - startTouchY);

        if (canforward && offset < -endThreshold && offsetY < yLimit) {
            Ao3TrackHelper.goForward();
        }
        else if (canbackward && offset >= endThreshold && offsetY < yLimit) {
            Ao3TrackHelper.goBack();
        }
        removeTouchEvents();
    }
    function touchCancelHandler(event: TouchEvent) {
        removeTouchEvents();
    }

    function removeTouchEvents() {
        Ao3TrackHelper.leftOffset = 0.0;
        //Ao3TrackHelper.opacity = 1.0;
        Ao3TrackHelper.showPrevPageIndicator = false;
        Ao3TrackHelper.showNextPageIndicator = false;
        document.removeEventListener("touchmove", touchMoveHandler);
        document.removeEventListener("touchend", touchEndHandler);
        document.removeEventListener("touchcancel", touchCancelHandler);
        window.removeEventListener("pointermove", pointerMoveHandler);
        window.removeEventListener("pointerup", pointerEndHandler);
        dragging = false;
    }

    function setTouchState() {
        let styles = getComputedStyle(document.documentElement, '');

        zoomFactor = window.screen.availWidth * window.screen.deviceXDPI / (window.screen.logicalXDPI * window.innerWidth);
        
        // If we can scroll horizontally, we disable swiping
        if (styles.msScrollLimitXMax !== styles.msScrollLimitXMin) {
            document.documentElement.classList.remove("mw_ao3track_unzoomed");
            document.documentElement.classList.add("mw_ao3track_zoomed");
            removeTouchEvents();
            if ('ontouchstart' in window) {
                document.removeEventListener("touchstart", touchStartHandler);
            }
            else if ('PointerEvent' in window) {
                window.removeEventListener("pointerdown", pointerDownHandler)
            }
        }
        else {
            document.documentElement.classList.remove("mw_ao3track_zoomed");
            document.documentElement.classList.add("mw_ao3track_unzoomed");
            removeTouchEvents();
            if ('ontouchstart' in window) {
                document.addEventListener("touchstart", touchStartHandler);
            }
            else if ('PointerEvent' in window) {
                window.addEventListener("pointerdown", pointerDownHandler)
            }
        }
    }

    document.addEventListener("MSContentZoom", (event) => {
        setTouchState();
    });
    setTouchState();

    function contextMenuHandler(ev: PointerEvent) {
        for (let target = ev.target as (HTMLElement|null); target && target !== document.body; target = target.parentElement) {
            let a = target as HTMLAnchorElement;
            if (target.tagName === "A" && a.href && a.href !== "") {
                ev.preventDefault();
                ev.stopPropagation();

                Ao3TrackHelper.showContextMenu(ev.clientX, ev.clientY, [
                    "Open", 
                    "Open and Add", 
                    "Add to Reading list", 
                    "Copy Link"
                ]).then((item) => {
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
                return;
            }
        }
    }
    document.body.addEventListener("contextmenu", contextMenuHandler);
    Ao3TrackHelper.setCookies(document.cookie);
};
