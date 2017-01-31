/// <reference path="../../../typings/globals/winjs/index.d.ts" />

declare var Ao3TrackHelper: Ao3Track.UWP.IAo3TrackHelper;

namespace Ao3Track {
    export namespace UWP {
        interface Native {
            native: never;
        }

        interface WorkChapterNative extends Native, IWorkChapter {
        }
        interface WorkChapterExNative extends WorkChapterNative, IWorkChapterEx {
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

        export interface IAo3TrackHelper {
            // Create native objects to pass back
            createWorkChapterMap(): IMap<number, WorkChapterNative>;
            createWorkChapter(number: number, chapterid: number, location: number | null, seq: number | null): WorkChapterNative;
            createWorkChapterEx(workid: number, number: number, chapterid: number, location: number | null, seq: number | null): WorkChapterExNative;

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
                Ao3TrackHelper.getWorkChaptersAsync(works).then((result) => {
                    callback(ToAssocArray<IWorkChapter>(result));
                });
            },

            setWorkChapters(workchapters: { [key: number]: IWorkChapter; }): void {
                var m = Ao3TrackHelper.createWorkChapterMap();
                for (let key in workchapters) {
                    m.insert(key as any, Ao3TrackHelper.createWorkChapter(workchapters[key].number, workchapters[key].chapterid, workchapters[key].location, workchapters[key].seq));
                }
                Ao3TrackHelper.setWorkChapters(m);
            },

            showContextMenu(x: number, y: number, menuItems: string[], callback: (selected: string | null)=>void): void {
                Ao3TrackHelper.showContextMenu(x,y,menuItems).then((selected)=> { callback(selected); } );
            },

            get currentLocation() : IWorkChapterEx | null { return Ao3TrackHelper.currentLocation; },
            set currentLocation(value : IWorkChapterEx | null)  { 
                if (value === null) { Ao3TrackHelper.currentLocation = null; }
                else { Ao3TrackHelper.currentLocation =  Ao3TrackHelper.createWorkChapterEx(value.workid,value.number,value.chapterid,value.location,value.seq);  }
            }
        };

        for(let name of Object.getOwnPropertyNames(Object.getPrototypeOf(Ao3TrackHelper)))
        {
            if (Object.getOwnPropertyDescriptor(Marshalled,name)) { continue; }
            let prop = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(Ao3TrackHelper),name);
            let newprop : PropertyDescriptor = { enumerable : prop.enumerable || false };
            if (typeof prop.value === "function")
            {
                newprop.value =  ()=>{
                    prop.value.apply(Ao3TrackHelper, arguments);
                };
            }
            else if ((typeof prop.value !== "null" && typeof prop.value !== "undefined") || prop.get || prop.set) 
            {
                if (prop.get || !prop.set)
                {
                    newprop.get = ()=>{
                        return (Ao3TrackHelper as any)[name];
                    };
                }
                if (!prop.get || prop.set)
                {
                    newprop.set = (value: any)=>{
                        return (Ao3TrackHelper as any)[name] = value;
                    };
                }
            }
            else {
                continue;
            }
            Object.defineProperty(Marshalled,name,newprop);
        }
    }
    export var Helper: Ao3Track.IAo3TrackHelper = Ao3Track.UWP.Marshalled as Ao3Track.IAo3TrackHelper;
}

