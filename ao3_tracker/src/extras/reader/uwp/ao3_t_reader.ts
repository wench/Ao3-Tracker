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
            m.insert(key as any, Ao3TrackHelper.createWorkChapter(workchapters[key].number, workchapters[key].chapterid, workchapters[key].location));
        }
        Ao3TrackHelper.setWorkChapters(m);
    }

    let nextPage : string | null = jQuery('head link[rel=next]').attr('href'); 
    export function SetNextPage(uri : string) {
        nextPage = uri;
    }

    let prevPage : string | null = jQuery('head link[rel=prev]').attr('href'); 
    export function SetPrevPage(uri : string) {
        prevPage = uri;
    }

    export function DisableLastLocationJump() {
        Ao3TrackHelper.enableJumpToLastLocation(false);
        Ao3TrackHelper.onjumptolastlocationevent = null;
    }

    export function EnableLastLocationJump(lastloc: IWorkChapter) {
        Ao3TrackHelper.onjumptolastlocationevent = (ev) => { Ao3Track.scrollToLocation(lastloc); }
        Ao3TrackHelper.enableJumpToLastLocation(true);
    }

    // Nonsense to allow for swiping back and foward between pages 

    function removeTouchEvents() {
        Ao3TrackHelper.leftOffset = 0.0;
        Ao3TrackHelper.opacity = 1.0;
        document.removeEventListener("touchmove", touchMoveHandler);
        document.removeEventListener("touchend", touchEndHandler);
        document.removeEventListener("touchcancel", touchCancelHandler);
    }
    let forward: boolean;
    let startTouchX : number = 0;
    function touchStartHandler(event: TouchEvent) {
        let touch = event.touches.item(0);
        if (event.touches.length > 1 || !touch) {
            removeTouchEvents();
            return;
        }
        startTouchX = touch.screenX / document.documentElement.msContentZoomFactor;
        let startLimit = window.innerWidth/4;
        if (Ao3TrackHelper.canGoBack && startTouchX < startLimit) {
            // going backwards....
            forward = false;
        }
        else if ((Ao3TrackHelper.canGoForward || (nextPage && nextPage !== '')) && startTouchX >= (window.innerWidth - startLimit)) {
            // Going forwards
            forward = true;
        }
        else {
            removeTouchEvents();
            return;
        }
        document.addEventListener("touchmove", touchMoveHandler);
        document.addEventListener("touchend", touchEndHandler);
        document.addEventListener("touchcancel", touchCancelHandler);
    }
    let lastTouchX : number = 0;
    function touchMoveHandler(event: TouchEvent) {
        let touch = event.touches.item(0);
        if (event.touches.length > 1 || !touch) {
            removeTouchEvents();
            return;
        }
        lastTouchX = touch.screenX / document.documentElement.msContentZoomFactor;
        let offset = lastTouchX - startTouchX;
        if (offset < -window.innerWidth / 2) {
            offset = -window.innerWidth / 2;
        }
        else if (offset > window.innerWidth / 2) {
            offset = window.innerWidth / 2;
        }
        if ((forward && offset > 0.0) || (!forward && offset < 0.0)) {
            offset = 0.0;
        }
        Ao3TrackHelper.leftOffset = offset;
        Ao3TrackHelper.opacity = (window.innerWidth - Math.abs(offset)) / window.innerWidth;
    }
    function touchEndHandler(event: TouchEvent) {
        if (forward && lastTouchX < window.innerWidth / 2) {
            if (Ao3TrackHelper.canGoForward) {
                window.history.forward();
            }
            else if (nextPage && nextPage !== '') {
                window.location.href = nextPage;
            }
        }
        else if (!forward && lastTouchX >= window.innerWidth / 2) {
            window.history.back();
        }
        else {
            removeTouchEvents();
        }
    }
    function touchCancelHandler(event: TouchEvent) {
        removeTouchEvents();
    }

    function setTouchState() {
        let zoomlimitmin: string = getComputedStyle(document.documentElement, '').msContentZoomLimitMin;
        var styles = getComputedStyle(document.body,'');
        if (document.documentElement.msContentZoomFactor > parseFloat(zoomlimitmin.slice(0, zoomlimitmin.indexOf('%'))) / 100.0) {
            document.documentElement.classList.remove("mw_ao3track_unzoomed");
            document.documentElement.classList.add("mw_ao3track_zoomed");
            removeTouchEvents();
            document.removeEventListener("touchstart", touchStartHandler);
        }
        else {
            document.documentElement.classList.remove("mw_ao3track_zoomed");
            document.documentElement.classList.add("mw_ao3track_unzoomed");
            removeTouchEvents();
            document.addEventListener("touchstart", touchStartHandler);
        }
    }

    document.addEventListener("MSContentZoom", (event) => {
        setTouchState();
    });
    setTouchState();
};
