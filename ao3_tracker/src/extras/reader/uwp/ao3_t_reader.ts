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

    let nextPage: string | null = jQuery('head link[rel=next]').attr('href');
    export function SetNextPage(uri: string) {
        nextPage = uri;
    }

    let prevPage: string | null = jQuery('head link[rel=prev]').attr('href');
    export function SetPrevPage(uri: string) {
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

    // Font size up/down support 
    let updatefontsize = () => {
        let inner = document.getElementById("inner");
        if (inner) {
            inner.style.fontSize = Ao3TrackHelper.fontSize.toString() + "%";
        }
    };
    Ao3TrackHelper.onalterfontsizeevent = updatefontsize;
    updatefontsize();

    // Nonsense to allow for swiping back and foward between pages 

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
    }
    let canforward = false;
    let canbackward = false;
    let startTouchX: number = 0;
    let startTouchY: number = 0;
    const startLimit = window.innerWidth;
    const endThreshold = window.innerWidth / 6;
    const maxSlide = window.innerWidth;
    const minThreshold = endThreshold / 4;
    const yLimit = window.innerHeight / 8;
    let zoomFactor = Ao3TrackHelper.realWidth / document.documentElement.clientWidth;
    interface TouchEventSubset extends Event {
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
            },
            bubbles: false,
            cancelBubble: false,
            cancelable: false,
            currentTarget: event.currentTarget,
            defaultPrevented: false,
            eventPhase: 0,
            isTrusted: false,
            returnValue: false,
            srcElement: event.srcElement,
            target: event.target,
            timeStamp: 0,
            type: "TouchEventSubset",
            initEvent: () => { },
            preventDefault: () => { },
            stopImmediatePropagation: () => { },
            stopPropagation: () => { },
            AT_TARGET: 0,
            BUBBLING_PHASE: 0,
            CAPTURING_PHASE: 0
        };

        touchStartHandler(te);

        window.addEventListener("pointermove", pointerMoveHandler);
        window.addEventListener("pointerup", pointerEndHandler);
    }
    function touchStartHandler(event: TouchEvent | TouchEventSubset) {
        let touch = event.touches.item(0);
        if (event.touches.length > 1 || !touch) {
            removeTouchEvents();
            return;
        }
        startTouchX = touch.screenX / zoomFactor;
        startTouchY = touch.screenY / zoomFactor;

        setImmediate(() => {
            canforward = false;
            canbackward = false;
            if (Ao3TrackHelper.canGoBack && startTouchX < startLimit) {
                // going backwards....
                canbackward = true;
            }
            if ((Ao3TrackHelper.canGoForward || (nextPage && nextPage !== '')) && startTouchX >= (window.innerWidth - startLimit)) {
                // Going forwards
                canforward = true;
            }
            if (!canbackward && !canforward) {
                removeTouchEvents();
                return;
            }
            if ('ontouchmove' in document) { document.addEventListener("touchmove", touchMoveHandler); }
            if ('ontouchend' in document) { document.addEventListener("touchend", touchEndHandler); }
            if ('ontouchcancel' in document) { document.addEventListener("touchcancel", touchCancelHandler); }
        });
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
            },
            bubbles: false,
            cancelBubble: false,
            cancelable: false,
            currentTarget: event.currentTarget,
            defaultPrevented: false,
            eventPhase: 0,
            isTrusted: false,
            returnValue: false,
            srcElement: event.srcElement,
            target: event.target,
            timeStamp: 0,
            type: "TouchEventSubset",
            initEvent: () => { },
            preventDefault: () => { },
            stopImmediatePropagation: () => { },
            stopPropagation: () => { },
            AT_TARGET: 0,
            BUBBLING_PHASE: 0,
            CAPTURING_PHASE: 0
        };

        touchMoveHandler(te);
    }
    function touchMoveHandler(event: TouchEvent | TouchEventSubset) {
        let touch = event.touches.item(0);
        if (event.touches.length > 1 || !touch) {
            removeTouchEvents();
            return;
        }
        lastTouchX = touch.screenX / zoomFactor;
        lastTouchY = touch.screenY / zoomFactor;
        setImmediate(() => {

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
        });
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
            },
            bubbles: false,
            cancelBubble: false,
            cancelable: false,
            currentTarget: event.currentTarget,
            defaultPrevented: false,
            eventPhase: 0,
            isTrusted: false,
            returnValue: false,
            srcElement: event.srcElement,
            target: event.target,
            timeStamp: 0,
            type: "TouchEventSubset",
            initEvent: () => { },
            preventDefault: () => { },
            stopImmediatePropagation: () => { },
            stopPropagation: () => { },
            AT_TARGET: 0,
            BUBBLING_PHASE: 0,
            CAPTURING_PHASE: 0
        };

        touchEndHandler(te);
    }
    function touchEndHandler(event: TouchEvent | TouchEventSubset) {
        setImmediate(() => {
            let offset = lastTouchX - startTouchX;
            let offsetY = Math.abs(lastTouchY - startTouchY);

            if (canforward && offset < -endThreshold && offsetY < yLimit) {
                if (Ao3TrackHelper.canGoForward) {
                    window.history.forward();
                }
                else if (nextPage && nextPage !== '') {
                    window.location.href = nextPage;
                }
            }
            else if (canbackward && offset >= endThreshold && offsetY < yLimit) {
                window.history.back();
            }
            else {
                removeTouchEvents();
            }
        });
    }
    function touchCancelHandler(event: TouchEvent) {
        removeTouchEvents();
    }

    function setTouchState() {
        let zoomlimitmin: string = getComputedStyle(document.documentElement, '').msContentZoomLimitMin;
        zoomFactor = document.documentElement.msContentZoomFactor * window.screen.deviceXDPI / window.screen.logicalXDPI;
        if (zoomFactor > parseFloat(zoomlimitmin.slice(0, zoomlimitmin.indexOf('%'))) / 100.0) {
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
};
