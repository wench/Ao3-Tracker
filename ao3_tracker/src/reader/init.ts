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

interface Window {
    jQuery: JQueryStatic;
}

namespace Ao3Track {
    export let jQuery = window.jQuery.noConflict(true);
    export let $ = jQuery;
    
    // Don't want wrapped function please. >:(
    function unWrapHR<T extends Function>(f: T, bindto?: object) : T
    {
        if (!f) return f;
        let ret : T;
        if ((f as any)["nr@original"]) ret = (f as any)["nr@original"];
        else ret = f;
        if (bindto) ret = ret.bind(bindto);
        return ret;
    }
    
    export let setImmediate = unWrapHR(window.setImmediate,window);
    export let setInterval = unWrapHR(window.setInterval,window);
    export let setTimeout = unWrapHR(window.setTimeout,window);
    export let clearImmediate = unWrapHR(window.clearImmediate,window);
    export let clearInterval = unWrapHR(window.clearInterval,window);
    export let clearTimeout = unWrapHR(window.clearTimeout,window);
    export let addEventListener = unWrapHR(EventTarget.prototype.addEventListener);
    export let removeEventListener = unWrapHR(EventTarget.prototype.removeEventListener);        
}
