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


interface MessageHandler
{
    postMessage: (message: Ao3Track.iOS.Message) => void;
}

interface MessageHandlers
{
    ao3track: MessageHandler;
}

interface WebKit
{
    messageHandlers: MessageHandlers;
}

interface Window
{
    webkit: WebKit;
}

namespace Ao3Track {
    export namespace iOS {
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

        // IOS doesn't doesn't a global containing the helper interface, instead it just sets a global containing the HelperDef

        export interface SetMessage
        {
            type: "SET";
            name: keyof IAo3TrackHelperProperties;
            value: string;
        }
        export interface CallMessage
        {
            type: "CALL";
            name: keyof IAo3TrackHelperMethods;
            args: string[];
        }

        export type Message = SetMessage | CallMessage;

        export let helperDef : Marshal.IHelperDef = Ao3TrackHelperNative;

        function serialize(val: any) {
            // If source is a string, then the value passes through unchanged. A minor optimization
            if (typeof val === "string") return val;
            return JSON.stringify(val);
        }

        export let helper = {  
            _values: { } as { [key: string]: any },

            setValue: function(name: keyof IAo3TrackHelperProperties, value: any) {
                this.values[name] = value;
            },

            _setValueInternal: function(name: keyof IAo3TrackHelperProperties, value: any) {
                this.values[name] = value;
                window.webkit.messageHandlers.ao3track.postMessage({ 
                    type: "SET",
                    name: name,
                    value: serialize(value)
                });
            },   

            _getValueInternal: function(name: keyof IAo3TrackHelperProperties) : any {
                return this.values[name];
            },    

            _callFunctionInternal: function(name: keyof IAo3TrackHelperMethods, args: any[]|IArguments) {
                let strArgs : string[] = [];
                for(let i = 0; i < args.length; i++)
                {
                    let a = args[i];
                    strArgs.push(serialize(a));
                }
                window.webkit.messageHandlers.ao3track.postMessage({ 
                    type: "CALL",
                    name: name,
                    args: strArgs                    
                });
            }     
        };
           
        // Fill the 'native' helper
        for (let name in helperDef) {
            let def = helperDef[name];

            if (def.args !== undefined) {
                let newprop: PropertyDescriptor = { enumerable: false };
                newprop.value = function(...args:any[]) {
                    helper._callFunctionInternal(name as any,arguments);
                };
                Object.defineProperty(helper, name, newprop);
            }
            else if (def.getter || def.setter) {
                let newprop: PropertyDescriptor = { enumerable: true };
                if (def.getter) newprop.get = () => helper._getValueInternal(name as any);
                if (def.setter) newprop.set = (value) => helper._setValueInternal(name as any,value);
                Object.defineProperty(helper, name, newprop);
            }
        }
    }
    Marshal.MarshalNativeHelper(iOS.helperDef, iOS.helper);
}