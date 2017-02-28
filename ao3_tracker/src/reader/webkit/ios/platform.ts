namespace Ao3Track {
    export namespace IOS {
        // Getting this to work for IOS will be a pain in the arse.
        //
        // WKWebView doesn't support exposing a native object to javascript code
        // It does support injecting scripts, evaluating scipts and has a js->native message notification system. Can't return a value from
        // the native message notification handler back to javascript. Argh! No indication if evaluating script code will have an instant 
        // effect. If it does, then easy, if not, there will be issues.
        //
        // Most of the code can be wrapped to work fine with those limitations as most of the data retrieval methods use async callbacks.
        // However the canGoBack, canGoForward and leftOffset properties are currently read by touch.ts. 
        //
        // All three can be worked around
        //
        // canGoBack, canGoForward only need to be set once right at the beginning of page load
        //
        // leftOffset is a little more complicated. Don't really want to call into js everytime it updates, but it's one possibility. Alternitavely 
        // change it from a property to methods, with the get being async 
    }
}