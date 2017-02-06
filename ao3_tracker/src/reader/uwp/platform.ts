/// <reference path="../../../typings/globals/winjs/index.d.ts" />

var Ao3TrackHelperUWP: Ao3Track.UWP.IAo3TrackHelperUWP;

namespace Ao3Track {
    export namespace UWP {
        interface Native {
            native: never;
        }

        interface WorkChapterNative extends Native, IWorkChapter {
        }
        interface WorkChapterExNative extends WorkChapterNative, IWorkChapterEx {
        }
        interface PageTitleNative extends Native, IPageTitle
        {
        }        

        interface IKeyValuePair<K, V> {
            key: K;
            value: V;
        }

        interface IIterator<T> {
            readonly current: T;
            getMany: () => any;
            readonly hasCurrent: boolean;
            moveNext: () => boolean;
        }
        interface IIterable<T> {
            first: () => IIterator<T>;
        }

        interface IMapView<K, V> extends IIterable<IKeyValuePair<K, V>> {
            hasKey: (key: K) => boolean;
            lookup: (key: K) => V;
            size: number;
            split: () => { first: IMapView<K, V>; second: IMapView<K, V> };
        }

        interface IMap<K, V> extends IIterable<IKeyValuePair<K, V>> {
            clear: () => void;
            getView: () => IMapView<K, V>;
            hasKey: (key: K) => boolean;
            insert: (key: K, value: V) => boolean;
            lookup: (key: K) => V;
            remove: (key: K) => void;
            size: number;
        }

        interface ClassNameMap
        {
            "WorkChapter" : WorkChapterNative;
            "WorkChapterEx" : WorkChapterExNative;
            "WorkChapterMap" : IMap<number, WorkChapterNative>;
            "PageTitle" : PageTitleNative;
        }

        export interface IAo3TrackHelperUWP {
            // Create native objects to pass back           
            createObject<K extends keyof ClassNameMap>(classname: K): ClassNameMap[K];

            getWorkChaptersAsync(works: number[]): WinJS.Promise<IMap<number, WorkChapterNative>>;
            setWorkChapters(workchapters: IMap<number, WorkChapterNative>): void;

            onjumptolastlocationevent: ((pagejump: boolean) => void) | null;
            jumpToLastLocationEnabled: boolean;

            nextPage: string;
            prevPage: string;
            canGoBack: boolean;
            canGoForward: boolean;
            goBack(): void;
            goForward(): void;
            leftOffset: number;
            opacity: number;
            showPrevPageIndicator: boolean;
            showNextPageIndicator: boolean;

            onalterfontsizeevent: ((ev: any) => void) | null;
            fontSize: number;

            showContextMenu(x: number, y: number, menuItems: string[]): WinJS.Promise<string | null>;
            addToReadingList(href: string): void;
            copyToClipboard(str: string, type: string): void;
            setCookies(cookies: string): void;

            currentLocation: WorkChapterExNative | null;
            pageTitle : PageTitleNative | null;
        }

        export function ToAssocArray<V>(map: IIterable<IKeyValuePair<number, V>>): { [key: number]: V } {
            var response: { [key: number]: V } = {};
            for (var it = map.first(); it.hasCurrent; it.moveNext()) {
                var i = it.current;
                response[i.key] = i.value;
            }
            return response;
        }

        export var Marshalled = {
            getWorkChaptersAsync(works: number[], callback: (workchapters: GetWorkChaptersMessageResponse) => void): void {
                Ao3TrackHelperUWP.getWorkChaptersAsync(works).then((result) => {
                    callback(ToAssocArray<IWorkChapter>(result));
                });
            },

            setWorkChapters(workchapters: { [key: number]: IWorkChapter; }): void {
                var m = Ao3TrackHelperUWP.createObject("WorkChapterMap");
                for (let key in workchapters) {
                    var obj = Ao3TrackHelperUWP.createObject("WorkChapter");
                    Object.assign(obj,workchapters[key]);
                    m.insert(key as any, obj);
                }
                Ao3TrackHelperUWP.setWorkChapters(m);
            },

            showContextMenu(x: number, y: number, menuItems: string[], callback: (selected: string | null)=>void): void {
                Ao3TrackHelperUWP.showContextMenu(x,y,menuItems).then((selected)=> { callback(selected); } );
            },

            get currentLocation() : IWorkChapterEx | null { return Ao3TrackHelperUWP.currentLocation; },
            set currentLocation(value : IWorkChapterEx | null)  { 
                if (value === null) { 
                    Ao3TrackHelperUWP.currentLocation = null; 
                }
                else { 
                    var obj = Ao3TrackHelperUWP.createObject("WorkChapterEx");
                    Object.assign(obj,value);                                    
                    Ao3TrackHelperUWP.currentLocation = obj;  
                }
            },

            get pageTitle(): IPageTitle | null { return Ao3TrackHelperUWP.pageTitle; },
            set pageTitle(value :IPageTitle | null) { 
                if (value === null) { 
                    Ao3TrackHelperUWP.pageTitle = null; 
                }
                else { 
                    var obj = Ao3TrackHelperUWP.createObject("PageTitle");
                    Object.assign(obj,value);                                    
                    Ao3TrackHelperUWP.pageTitle = obj;  
                }                
            }
        };

        for(let name of Object.getOwnPropertyNames(Object.getPrototypeOf(Ao3TrackHelperUWP)))
        {
            if (Object.getOwnPropertyDescriptor(Marshalled,name)) { continue; }
            let prop = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(Ao3TrackHelperUWP),name);
            let newprop : PropertyDescriptor = { enumerable : prop.enumerable || false };
            if (typeof prop.value === "function")
            {
                newprop.value =  ()=>{
                    prop.value.apply(Ao3TrackHelperUWP, arguments);
                };
            }
            else if ((typeof prop.value !== "null" && typeof prop.value !== "undefined") || prop.get || prop.set) 
            {
                if (prop.get || !prop.set)
                {
                    newprop.get = ()=>{
                        return (Ao3TrackHelperUWP as any)[name];
                    };
                }
                if (!prop.get || prop.set)
                {
                    newprop.set = (value: any)=>{
                        return (Ao3TrackHelperUWP as any)[name] = value;
                    };
                }
            }
            else {
                continue;
            }
            Object.defineProperty(Marshalled,name,newprop);
        }
    }
    Helper = Ao3Track.UWP.Marshalled as any as Ao3Track.IAo3TrackHelper;
}

