interface Message
{
    type: "SET"|"CALL";
    name: string;
    data: string;
}

interface MessageHandler
{
    postMessage: (message: Message) => void;
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

        export let helperDef : Marshal.IHelperDef = JSON.parse(Ao3TrackHelperNative);

        export let helper = {  
            _values: { } as { [key: string]: any },
            setValue: function(name: string, value: any) {
                this.values[name] = value;
            },

            _setValueInternal: function(name: string, value: any) {
                this.values[name] = value;
                window.webkit.messageHandlers.ao3track.postMessage({ 
                    type: "SET",
                    name: name,
                    data: JSON.stringify(value)
                });
            },   

            _getValueInternal: function(name: string) : any {
                return this.values[name];
            },    

            _callFunctionInternal: function(name: string, args: any[]) {
                window.webkit.messageHandlers.ao3track.postMessage({ 
                    type: "CALL",
                    name: name,
                    data: JSON.stringify(Object.assign({}, args))
                });
            }     
        };
           
        // Fill the 'native' helper
        for (let name in helperDef) {
            let def = helperDef[name];

            if (def.args !== undefined) {
                let newprop: PropertyDescriptor = { enumerable: false };
                newprop.value = function(...args:any[]) {
                    helper._callFunctionInternal(name,[].slice.call(arguments));
                };
                Object.defineProperty(helper, name, newprop);
            }
            else if (def.getter || def.setter) {
                let newprop: PropertyDescriptor = { enumerable: true };
                if (def.getter) newprop.get = () => helper._getValueInternal(name);
                if (def.setter) newprop.set = (value) => helper._setValueInternal(name,value);
                Object.defineProperty(helper, name, newprop);
            }
        }
    }
    Marshal.MarshalNativeHelper(iOS.helperDef, iOS.helper);
}