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
    export namespace Messaging {
        export let notify : (json : string) => void;

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

        // IOS doesn't have a global containing the helper interface, instead it just sets a global containing the HelperDef
        export let helperDef : Marshal.IHelperDef = Ao3TrackHelperNative;

        function serialize(val: any) {
            // If source is a string, then the value passes through unchanged. A minor optimization
            if (typeof val === "string") return val;
            return JSON.stringify(val);
        }

        export let helper = {  
            _values: { } as { [key: string]: any },

            setValue: function(name: keyof IAo3TrackHelperProperties, value: any) {
                helper._values[name] = value;
            },

            _setValueInternal: function(name: keyof IAo3TrackHelperProperties, value: any) {
                helper._values[name] = value;
                Messaging.notify(JSON.stringify({ 
                    type: "SET",
                    name: name,
                    value: serialize(value)
                }));
            },   

            _getValueInternal: function(name: keyof IAo3TrackHelperProperties) : any {
                return helper._values[name];
            },    

            _callFunctionInternal: function(name: keyof IAo3TrackHelperMethods, args: any[]|IArguments) {
                let strArgs : string[] = [];
                for(let i = 0; i < args.length; i++)
                {
                    let a = args[i];
                    strArgs.push(serialize(a));
                }
                Messaging.notify(JSON.stringify({ 
                    type: "CALL",
                    name: name,
                    args: strArgs                    
                }));
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
    Marshal.MarshalNativeHelper(Messaging.helperDef, Messaging.helper);
}
