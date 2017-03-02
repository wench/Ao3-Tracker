namespace Ao3Track {
    export namespace Touch {
       
        // Nonsense to allow for swiping back and foward between pages 

        let startTouchX: number = 0;
        let startTouchY: number = 0;
        let startTime : number = 0;
        let lastTime : number = 0;
        let lastTouchX: number = 0;
        let lastTouchY: number = 0;
        let minThreshold: number = 0;
        let centre: number = 0;
        let end: number = 0;
        let yLimit: number = 0;
        let coordFixup: number = 0;
        let velocity : number = 0;

        function swipeCleanup(keepOffset?: boolean) {
            Helper.stopWebViewDragAccelerate();
            if (!keepOffset) Ao3Track.Helper.leftOffset = 0.0;
            Ao3Track.Helper.showPrevPageIndicator = 0;
            Ao3Track.Helper.showNextPageIndicator = 0;
            window.removeEventListener("touchmove", touchMoveHandler);
            window.removeEventListener("touchend", touchEndHandler);
            window.removeEventListener("touchcancel", touchCancelHandler);
            window.removeEventListener("pointermove", pointerMoveHandler);
            window.removeEventListener("pointerup", pointerEndHandler);
            window.removeEventListener("pointercancel", pointerCancelHandler);
        }

        function swipeStart(x : number, y : number) : boolean
        {
            coordFixup = Ao3Track.Helper.deviceWidth / (window.innerWidth * (window.screen.deviceXDPI||192) / (window.screen.logicalXDPI||192));
            end = Ao3Track.Helper.deviceWidth;
            minThreshold = end / 24;
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

            let now = performance.now() ;
            if (now <= lastTime) now = lastTime + 1;
            
            velocity = (touchX-lastTouchX) * 1000.0 / (now-lastTime); // pixels/s
            lastTouchX = touchX;
            lastTime = now;

            let offsetCat = swipeOffsetChanged(offset, offsetY);
            
            if (offsetCat === 0) return false;

            return true;
        }

        function swipeEnd(x : number, y : number) : boolean
        {
            let offset = lastTouchX - startTouchX;
            let offsetY = Math.abs(lastTouchY - startTouchY);            
            /*
            let touchX = x*coordFixup;
            lastTouchY = y*coordFixup;

            let offset = touchX - startTouchX;
            let offsetY = Math.abs(lastTouchY - startTouchY);

            if ((!Ao3Track.Helper.canGoBack && offset > 0.0) || (!Ao3Track.Helper.canGoForward && offset < 0.0) || (offset > 0.0 && offset < minThreshold) || (offset < 0.0 && offset > -minThreshold) ||
                (offsetY >= yLimit) || offset === 0) {
                swipeCleanup();
                return false;
            }

            let now = performance.now();
            if (now <= lastTime) now = lastTime + 1;
            
            if (touchX !== lastTouchX)
            {
                velocity = (touchX-lastTouchX) * 1000.0 / (now-lastTime); // pixels/s
                lastTouchX = touchX;
            }
            */

            swipeCleanup(true);

            let offsetCat = swipeOffsetChanged(offset, offsetY);
            if (offsetCat === 3) {
                Ao3Track.Helper.goBack();
                return true;
            }
            else if (offsetCat === -3) {
                Ao3Track.Helper.goForward();
                return true;
            }
            else if (offsetCat === 0) {
                if (Math.abs(offset) < 8 && offsetY < 8 && (performance.now() - startTime) > 1000) {
                    let devToClient = window.innerWidth / Ao3Track.Helper.deviceWidth;
                    let ev = new MouseEvent("contextmenu",{
                        clientX: (lastTouchX * devToClient) - window.screenLeft,
                        clientY: (lastTouchY * devToClient) - window.screenTop,
                        bubbles: true,
                        cancelable: true,
                        button: 0   
                    });
                    let el = document.elementFromPoint(ev.clientX,ev.clientY);
                    if (el) {
                        el.dispatchEvent(ev);
                        if (ev.defaultPrevented) return true;
                    }
                }
                swipeCleanup();
                return false;
            }
            
            Helper.startWebViewDragAccelerate(velocity);
            
            return true;
        }   

        function swipeOffsetChanged(offset : number, offsetY : number) : number
        {
            if ((!Ao3Track.Helper.canGoBack && offset > 0.0) || (!Ao3Track.Helper.canGoForward && offset < 0.0) || (offset > 0.0 && offset < minThreshold) || (offset < 0.0 && offset > -minThreshold) ||
                (offsetY >= yLimit)) {
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
            

            if (Ao3Track.Helper.canGoForward && offset < -centre && offsetY < yLimit) {
                Ao3Track.Helper.showNextPageIndicator = 2;
                if (offset <= -end) return -3;
                return -2;
            }
            else if (Ao3Track.Helper.canGoForward && offset < 0 && offsetY < yLimit) {
                Ao3Track.Helper.showNextPageIndicator = 1;
            }
            else {
                Ao3Track.Helper.showNextPageIndicator = 0;
            }

            if (Ao3Track.Helper.canGoBack && offset >= centre && offsetY < yLimit) {
                Ao3Track.Helper.showPrevPageIndicator = 2;
                if (offset >= end) return 3;
                return 2;
            }
            else if (Ao3Track.Helper.canGoBack && offset > 0 && offsetY < yLimit)
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
                window.addEventListener("pointermove", pointerMoveHandler);
                window.addEventListener("pointerup", pointerEndHandler);
                window.addEventListener("pointercancel", pointerCancelHandler);                
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

            if (swipeEnd(event.screenX,event.screenY)) {
                event.preventDefault();
                let handle = (ev: MouseEvent) => {
                    ev.preventDefault();
                    event.target.removeEventListener("click",handle);
                };

                event.target.addEventListener("click",handle);
            }
        }
        function pointerCancelHandler(event: PointerEvent) {
            swipeCleanup();
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
                document.addEventListener("touchmove", touchMoveHandler); 
                document.addEventListener("touchend", touchEndHandler); 
                document.addEventListener("touchcancel", touchCancelHandler);
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
            if (swipeEnd(touch.screenX,touch.screenY)) {
                event.preventDefault();
                let handle = (ev: MouseEvent) => {
                    ev.preventDefault();
                    event.target.removeEventListener("click",handle);
                };

                event.target.addEventListener("click",handle);
            }
        }
        function touchCancelHandler(event: TouchEvent) {
            swipeCleanup();
        }

        export function updateTouchState() {
            let styles = getComputedStyle(document.documentElement, '');

            // If we can scroll horizontally, we disable swiping
            if (styles.msScrollLimitXMax !== styles.msScrollLimitXMin || document.documentElement.clientWidth !== window.innerWidth) {
                document.documentElement.classList.remove("mw_ao3track_unzoomed");
                document.documentElement.classList.add("mw_ao3track_zoomed");
                swipeCleanup();
                if ('ontouchstart' in window) {
                    window.removeEventListener("touchstart", touchStartHandler);
                }
                else if ('PointerEvent' in window) {
                    window.removeEventListener("pointerdown", pointerDownHandler);
                }
            }
            else {
                document.documentElement.classList.remove("mw_ao3track_zoomed");
                document.documentElement.classList.add("mw_ao3track_unzoomed");
                swipeCleanup();
                if ('ontouchstart' in window) {
                    window.addEventListener("touchstart", touchStartHandler);
                }
                else if ('PointerEvent' in window) {
                    window.addEventListener("pointerdown", pointerDownHandler);
                }
            }
        }

        document.addEventListener("MSContentZoom", (event) => {
            updateTouchState();
        });
        document.addEventListener("resize", (event) => {
            updateTouchState();
        });        
        updateTouchState();
    }
}