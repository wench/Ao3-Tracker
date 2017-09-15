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
    export namespace Touch {
       
        // Nonsense to allow for swiping back and foward between pages 

        let startTouchX: number = 0;
        let startTouchY: number = 0;
        let startTime : number = 0;
        let lastTime : number = 0;
        let lastTouchX: number = 0;
        let lastTouchY: number = 0;
        let startThreshold: number = 0;
        let centre: number = 0;
        let end: number = 0;
        let yLimit: number = 0;
        let coordFixup: number = 0;
        let velocity : number = 0;
        let manualContextMenu = false;


        namespace performance {
            export function now() {
                if (window.performance.now) return window.performance.now();
                else return Date.now();
            }
        }

        if (!window.performance.now) 
            manualContextMenu = true;

        function swipeCleanup(keepOffset?: boolean) {
            Helper.stopWebViewDragAccelerate();
            if (!keepOffset) Ao3Track.Helper.leftOffset = 0.0;
            Ao3Track.Helper.showPrevPageIndicator = 0;
            Ao3Track.Helper.showNextPageIndicator = 0;
            removeEventListener.window("touchmove", touchMoveHandler);
            removeEventListener.window("touchend", touchEndHandler);
            removeEventListener.window("touchcancel", touchCancelHandler);
            removeEventListener.window("pointermove", pointerMoveHandler);
            removeEventListener.window("pointerup", pointerEndHandler);
            removeEventListener.window("pointercancel", pointerCancelHandler);
        }

        function swipeStart(x : number, y : number) : boolean
        {
            coordFixup = Ao3Track.Helper.deviceWidth / (window.innerWidth * (window.screen.deviceXDPI||192) / (window.screen.logicalXDPI||192));
            end = Ao3Track.Helper.deviceWidth;
            startThreshold = Math.min(end / 12, 160);
            yLimit = window.innerHeight / 8;
            centre = end / 2;
            Helper.stopWebViewDragAccelerate();

            startTime = lastTime = performance.now() ;
            lastTouchX = x*coordFixup;
            startTouchX = lastTouchX - Ao3Track.Helper.leftOffset; 
            lastTouchY = startTouchY = y*coordFixup;

/*
            if (!Ao3Track.Helper.canGoBack && !Ao3Track.Helper.canGoForward) {
                swipeCleanup();
                return false;
            }*/
            
            swipeOffsetChanged(lastTouchX - startTouchX, 0);

            return true;
        }

        function swipeMove(x : number, y : number) : boolean
        {
            let touchX = x*coordFixup;
            lastTouchY = y*coordFixup;

            let offset = touchX - startTouchX;
            let offsetY = Math.abs(lastTouchY - startTouchY);

            // Too much y movement? Disable this entirely 
            if (offsetY >= yLimit * 2) {
                swipeCleanup();
                return false;
            }

            // Touch much move at least startThreshold before swiping can occur.
            if (startThreshold !== 0) {
                if (offset <= -startThreshold) {
                    startTouchX -= startThreshold;
                    offset += startThreshold;
                    startThreshold = 0;
                }
                else if (offset >= startThreshold) {
                    startTouchX += startThreshold;
                    offset -= startThreshold;
                    startThreshold = 0;
                }
                else {
                    offset = 0;
                }
            }
            
            let now = performance.now() ;
            if (now <= lastTime) now = lastTime + 1;
            
            velocity = (touchX-lastTouchX) * 1000.0 / (now-lastTime); // pixels/s
            lastTouchX = touchX;
            lastTime = now;

            let offsetCat = swipeOffsetChanged(offset, offsetY);
            
            if (offsetCat === 0 && startThreshold !== 0) return false;

            return true;
        }

        function swipeEnd(clientX : number, clientY: number) : boolean
        {
            let offset = lastTouchX - startTouchX;
            let offsetY = Math.abs(lastTouchY - startTouchY);            

            // Touch much move at least startThreshold before swiping can occur.
            if (startThreshold !== 0) {
                if (offset <= -startThreshold) {
                    startTouchX -= startThreshold;
                    offset += startThreshold;
                    startThreshold = 0;
                }
                else if (offset >= startThreshold) {
                    startTouchX += startThreshold;
                    offset -= startThreshold;
                    startThreshold = 0;
                }
                else {
                    offset = 0;
                }
            }

            swipeCleanup(true);

            let offsetCat = swipeOffsetChanged(offset, offsetY);
            if (offsetCat === 0) {
                if (manualContextMenu && Math.abs(offset) < 8 && offsetY < 8 && (performance.now() - startTime) > 1000) {
                    let el = document.elementFromPoint(clientX,clientY);
                    if (el) {
                        if (contextMenuForAnchor(el,clientX,clientY)) 
                            return true;
                    }
                }
                swipeCleanup();
                return false;
            }
            
            Helper.startWebViewDragAccelerate(velocity);
            
            return false;
        }   

        function swipeOffsetChanged(offset : number, offsetY : number) : number
        {
            if ((!Ao3Track.Helper.swipeCanGoBack && offset > 0.0) || (!Ao3Track.Helper.swipeCanGoForward && offset < 0.0) || (offsetY >= yLimit)) {
                offset = 0.0;
            }
            else if (offset < -end) 
            {
                offset = -end;
            }
            else if (offset > end) 
            {
                offset = end;
            }
            Ao3Track.Helper.leftOffset = offset;
            

            if (Ao3Track.Helper.swipeCanGoForward && offset < -centre && offsetY < yLimit) {
                Ao3Track.Helper.showNextPageIndicator = 2;
                if (offset <= -end) return -3;
                return -2;
            }
            else if (Ao3Track.Helper.swipeCanGoForward && offset < 0 && offsetY < yLimit) {
                Ao3Track.Helper.showNextPageIndicator = 1;
            }
            else {
                Ao3Track.Helper.showNextPageIndicator = 0;
            }

            if (Ao3Track.Helper.swipeCanGoBack && offset >= centre && offsetY < yLimit) {
                Ao3Track.Helper.showPrevPageIndicator = 2;
                if (offset >= end) return 3;
                return 2;
            }
            else if (Ao3Track.Helper.swipeCanGoBack && offset > 0 && offsetY < yLimit)
            {
                Ao3Track.Helper.showPrevPageIndicator = 1;
            }
            else {
                Ao3Track.Helper.showPrevPageIndicator = 0;
            }

            if (offset === 0) return 0;                       
            else if (offset < 0) return -1;
            else return 1;
        }     

        //
        // Pointer Events
        //
        let pointerEventsAny = true;
        let pointerEventsType = "";
        function pointerDownHandler(event: PointerEvent) {
            // Only Touch
            if (!pointerEventsAny && event.pointerType !== "touch") {
                swipeCleanup();
                return;
            }

            if (pointerEventsType !== "" && pointerEventsType !== event.pointerType)
            {
                swipeCleanup();
                return;
            }
            pointerEventsType = event.pointerType;

            // Only primary
            if (!event.isPrimary) {
                swipeCleanup();
                return;
            }

            if (swipeStart(event.screenX,event.screenY)) {
                addEventListener.window("pointermove", pointerMoveHandler);
                addEventListener.window("pointerup", pointerEndHandler);
                addEventListener.window("pointercancel", pointerCancelHandler);                
            }
        }
        function pointerMoveHandler(event: PointerEvent) {
            // Only Touch
            if (!pointerEventsAny && event.pointerType !== "touch") {
                swipeCleanup();
                return;
            }
            if (pointerEventsType !== event.pointerType)
            {
                swipeCleanup();
                return;
            }         

            // Only primary
            if (!event.isPrimary) {
                swipeCleanup();
                return;
            }

            if (swipeMove(event.screenX,event.screenY))
                event.preventDefault();
        }
        function pointerEndHandler(event: PointerEvent) {
            // Only Touch
            if (!pointerEventsAny && event.pointerType !== "touch") {
                swipeCleanup();
                return;
            }
            if (pointerEventsType !== event.pointerType)
            {
                swipeCleanup();
                return;
            }

            // Only primary
            if (!event.isPrimary) {
                swipeCleanup();
                return;
            }

            if (swipeEnd(event.clientX, event.clientY)) {
                event.preventDefault();
                let limit = Date.now() + 100;
                let handle = (ev: MouseEvent) => {
                    if (Date.now() <= limit)  ev.preventDefault();
                    removeEventListener.call(event.target,"click",handle);
                };

                addEventListener.call(event.target,"click",handle);
            }
        }
        function pointerCancelHandler(event: PointerEvent) {
            if (swipeEnd(event.clientX, event.clientY)) {
                event.preventDefault();
                let limit = Date.now() + 100;
                let handle = (ev: MouseEvent) => {
                    if (Date.now() <= limit)  ev.preventDefault();
                    removeEventListener.call(event.target,"click",handle);
                };

                addEventListener.call(event.target,"click",handle);
            }
        }

        //
        // Touch Events
        //

        function touchStartHandler(event: TouchEvent) {
            let touch = event.touches.item(0);
            if (event.touches.length > 1 || !touch) {
                swipeCleanup();
                return;
            }
            if (swipeStart(touch.screenX,touch.screenY)) {
                addEventListener.document("touchmove", touchMoveHandler); 
                addEventListener.document("touchend", touchEndHandler); 
                addEventListener.document("touchcancel", touchCancelHandler);
            }
        }

        function touchMoveHandler(event: TouchEvent) {
            let touch = event.changedTouches.item(0);
            if (event.touches.length > 1 || !touch) {
                swipeCleanup();
                return;
            }

            if (swipeMove(touch.screenX,touch.screenY))
                event.preventDefault();
        }


        function touchEndHandler(event: TouchEvent) {
            let touch = event.changedTouches.item(0);
            if (event.touches.length > 1 || !touch) {
                swipeCleanup();
                return;
            }
            if (swipeEnd(touch.clientX, touch.clientY)) {
                event.preventDefault();
                let limit = Date.now() + 100;
                let handle = (ev: MouseEvent) => {
                    if (Date.now() <= limit)  ev.preventDefault();
                    removeEventListener.call(event.target,"click",handle);
                };

                addEventListener.call(event.target,"click",handle);
            }
        }
        function touchCancelHandler(event: TouchEvent) {
            let touch = event.changedTouches.item(0) || {clientX: Number.NEGATIVE_INFINITY, clientY: Number.NEGATIVE_INFINITY};

            if (swipeEnd(touch.clientX, touch.clientY)) {
                event.preventDefault();
                let limit = Date.now() + 100;
                let handle = (ev: MouseEvent) => {
                    if (Date.now() <= limit)  ev.preventDefault();
                    removeEventListener.call(event.target,"click",handle);
                };

                addEventListener.call(event.target,"click",handle);
            }
        }

        export function updateTouchState() {
            let styles = getComputedStyle(document.documentElement, '');

            // If we can scroll horizontally, we disable swiping
            if (styles.msScrollLimitXMax !== styles.msScrollLimitXMin || document.documentElement.clientWidth !== window.innerWidth) {
                document.documentElement.classList.remove("mw_ao3track_unzoomed");
                document.documentElement.classList.add("mw_ao3track_zoomed");
                swipeCleanup();
                if ('ontouchstart' in window) {
                    removeEventListener.window("touchstart", touchStartHandler);
                }
                else if ('PointerEvent' in window) {
                    removeEventListener.window("pointerdown", pointerDownHandler);
                }
            }
            else {
                document.documentElement.classList.remove("mw_ao3track_zoomed");
                document.documentElement.classList.add("mw_ao3track_unzoomed");
                swipeCleanup();
                if ('ontouchstart' in window) {
                    addEventListener.window("touchstart", touchStartHandler);
                }
                else if ('PointerEvent' in window) {
                    addEventListener.window("pointerdown", pointerDownHandler);
                }
            }
        }

        addEventListener.document("MSContentZoom", () => {
            Ao3Track.Touch.updateTouchState();
        });
        addEventListener.document("resize", () => {
            Ao3Track.Touch.updateTouchState();
        });        
        updateTouchState();
    }
}