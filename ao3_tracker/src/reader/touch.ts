namespace Ao3Track {
    namespace Touch {

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
            if (Ao3Track.Helper.canGoBack && startTouchX < startLimit) {
                // going backwards....
                canbackward = true;
            }
            if (Ao3Track.Helper.canGoForward && startTouchX >= (window.innerWidth - startLimit)) {
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
                Ao3Track.Helper.showNextPageIndicator = true;
            }
            else {
                Ao3Track.Helper.showNextPageIndicator = false;
            }

            if (canbackward && offset >= endThreshold && offsetY < yLimit) {
                Ao3Track.Helper.showPrevPageIndicator = true;
            }
            else {
                Ao3Track.Helper.showPrevPageIndicator = false;
            }

            Ao3Track.Helper.leftOffset = offset;
            //Ao3Track.Helper.opacity = (window.innerWidth - Math.abs(offset)) / window.innerWidth;
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
                Ao3Track.Helper.goForward();
            }
            else if (canbackward && offset >= endThreshold && offsetY < yLimit) {
                Ao3Track.Helper.goBack();
            }
            removeTouchEvents();
        }
        function touchCancelHandler(event: TouchEvent) {
            removeTouchEvents();
        }

        function removeTouchEvents() {
            Ao3Track.Helper.leftOffset = 0.0;
            //Ao3Track.Helper.opacity = 1.0;
            Ao3Track.Helper.showPrevPageIndicator = false;
            Ao3Track.Helper.showNextPageIndicator = false;
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

    }
}