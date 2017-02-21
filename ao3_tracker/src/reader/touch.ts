namespace Ao3Track {
    namespace Touch {
       
        // Nonsense to allow for swiping back and foward between pages 

        let startTouchX: number = 0;
        let startTouchY: number = 0;
        let lastTime : number = 0;
        let lastTouchX: number = 0;
        let lastTouchY: number = 0;
        let minThreshold: number = 0;
        let centre: number = 0;
        let end: number = 0;
        let yLimit: number = 0;
        let zoomFactor: number = 0;
        let momentumInterval : number = 0;
        let velocity : number = 0;

        function swipeCleanup(keepOffset?: boolean) {
            if (momentumInterval !== 0) clearInterval(momentumInterval);
            momentumInterval = 0;
            if (!keepOffset) Ao3Track.Helper.leftOffset = 0.0;
            Ao3Track.Helper.showPrevPageIndicator = 0;
            Ao3Track.Helper.showNextPageIndicator = 0;
            document.removeEventListener("touchmove", touchMoveHandler);
            document.removeEventListener("touchend", touchEndHandler);
            document.removeEventListener("touchcancel", touchCancelHandler);
            window.removeEventListener("pointermove", pointerMoveHandler);
            window.removeEventListener("pointerup", pointerEndHandler);
            window.removeEventListener("pointercancel", pointerCancelHandler);
        }

        function swipeStart(x : number, y : number) : boolean
        {
            zoomFactor = window.devicePixelRatio;
            minThreshold = window.innerWidth / 24;
            yLimit = window.innerHeight / 8;
            centre = window.innerWidth / 2;
            end = window.innerWidth;
            if (momentumInterval !== 0) clearInterval(momentumInterval);
            momentumInterval = 0;

            lastTime = Date.now();
            lastTouchX =x/zoomFactor;
            startTouchX = lastTouchX - Ao3Track.Helper.leftOffset; 
            lastTouchY = startTouchY = y/zoomFactor;

            if (!Ao3Track.Helper.canGoBack && !Ao3Track.Helper.canGoForward) {
                swipeCleanup();
                return false;
            }
            
            swipeOffsetChanged(lastTouchX - startTouchX, 0);

            return true;
        }

        function swipeMove(x : number, y : number) : boolean
        {
            let touchX = x/zoomFactor;
            lastTouchY = y/zoomFactor;

            let offset = touchX - startTouchX;
            let offsetY = Math.abs(lastTouchY - startTouchY);

            // Too much y movement? Disable this entirely 
            if (offsetY >= yLimit * 2) {
                swipeCleanup();
                return false;
            }

            let now = Date.now();
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
            let touchX = x/zoomFactor;
            lastTouchY = y/zoomFactor;

            let offset = touchX - startTouchX;
            let offsetY = Math.abs(lastTouchY - startTouchY);

            if ((!Ao3Track.Helper.canGoBack && offset > 0.0) || (!Ao3Track.Helper.canGoForward && offset < 0.0) || (offset > 0.0 && offset < minThreshold) || (offset < 0.0 && offset > -minThreshold) ||
                (offsetY >= yLimit) || offset === 0) {
                swipeCleanup();
                return false;
            }

            let now = Date.now();
            if (now <= lastTime) now = lastTime + 1;
            
            if (touchX !== lastTouchX)
            {
                velocity = (touchX-lastTouchX) * 1000.0 / (now-lastTime); // pixels/s
                lastTouchX = touchX;
            }

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
            
            momentumInterval = setInterval(() => {
                lastTime = now;
                now = Date.now();
                if (now <= lastTime) now = lastTime + 1;

                let acceleration = 0;   // pixels/s^2

                if (offsetCat <= -2) acceleration = -4000.0;
                else if (offsetCat === -1) acceleration = 4000.0;
                else if (offsetCat >= 2) acceleration = 4000.0;
                else if (offsetCat === 1) acceleration = -4000.0;
                else {
                    swipeCleanup();
                    return;
                }

                let oldoffset = offset;
                velocity = velocity + acceleration * (now-lastTime) / 1000.0;
                offset = offset + velocity * (now-lastTime) / 1000.0;

                if ((oldoffset < 0 && offset >= 0) || (oldoffset > 0 && offset <= 0))
                {
                    swipeCleanup();
                    return;
                }

                offsetCat = swipeOffsetChanged(offset, offsetY);
                if (offsetCat === 3) {
                    swipeCleanup(true);
                    Ao3Track.Helper.goBack();
                    return;
                }
                else if (offsetCat === -3) {
                    swipeCleanup(true);
                    Ao3Track.Helper.goForward();
                    return;
                }                
            }, 10);
            
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

            if (swipeEnd(event.screenX,event.screenY))
                event.preventDefault();            
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
            if (swipeEnd(touch.screenX,touch.screenY))
                event.preventDefault();
        }
        function touchCancelHandler(event: TouchEvent) {
            swipeCleanup();
        }

        function setTouchState() {
            let styles = getComputedStyle(document.documentElement, '');

            // If we can scroll horizontally, we disable swiping
            if (styles.msScrollLimitXMax !== styles.msScrollLimitXMin) {
                document.documentElement.classList.remove("mw_ao3track_unzoomed");
                document.documentElement.classList.add("mw_ao3track_zoomed");
                swipeCleanup();
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
                swipeCleanup();
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
        document.addEventListener("resize", (event) => {
            setTouchState();
        });        
        setTouchState();
    }
}