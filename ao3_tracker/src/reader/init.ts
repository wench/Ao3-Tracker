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
    export function unWrapNR<T extends Function>(f: T, bindto?: object) : T
    {
        if (!f) return f;
        let ret : T;
        if ((f as any)["nr@original"]) ret = (f as any)["nr@original"];
        else ret = f;
        if (bindto) ret = ret.bind(bindto);
        return ret;
    }

    class UnwrappedEventListenerFunc {
        funcName: string;

        constructor(funcName: string)
        {
            this.funcName = funcName;
            this.window = this.get(window).bind(window);
            this.document = this.get(document).bind(document);
        }

        get(obj: object) : (type: string, listener?: EventListenerOrEventListenerObject, options?: boolean) => void
        {
            for (let proto = obj; proto !== null; proto = Object.getPrototypeOf(proto)) {
                let props = Object.getOwnPropertyNames(proto);
                if (props.indexOf(this.funcName) !== -1) {
                    return unWrapNR((proto as any)[this.funcName]);
                }        
            }            
            return () => { }
        }

        window: (type: string, listener?: EventListenerOrEventListenerObject, options?: boolean) => void;
        document: (type: string, listener?: EventListenerOrEventListenerObject, options?: boolean) => void;
        call (target: object, type: string, listener?: EventListenerOrEventListenerObject, options?: boolean) : void
        {
            this.get(target).call(target, type, listener||undefined, options||undefined);
        }
    };
    
    export let setImmediate = unWrapNR(window.setImmediate,window);
    export let setInterval = unWrapNR(window.setInterval,window);
    export let setTimeout = unWrapNR(window.setTimeout,window);
    export let clearImmediate = unWrapNR(window.clearImmediate,window);
    export let clearInterval = unWrapNR(window.clearInterval,window);
    export let clearTimeout = unWrapNR(window.clearTimeout,window);
    export let addEventListener = new UnwrappedEventListenerFunc("addEventListener");
    export let removeEventListener = new UnwrappedEventListenerFunc("removeEventListener");
}
