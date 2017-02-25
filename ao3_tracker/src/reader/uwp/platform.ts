/// <reference path="../../../typings/globals/winjs/index.d.ts" />

// tslint:disable-next-line:no-var-keyword
var Ao3TrackHelperUWP: Ao3Track.UWP.IAo3TrackHelperUWP;

namespace Ao3Track {
    Helper = {} as Ao3Track.IAo3TrackHelper;

    export namespace UWP {
        interface Native {
            native: never;
        }

        interface WorkChapterNative extends Native, IWorkChapter {
        }
        interface WorkChapterExNative extends WorkChapterNative, IWorkChapterEx {
        }
        interface PageTitleNative extends Native, IPageTitle {
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

        interface ClassNameMap {
            "WorkChapterNative": WorkChapterNative;
            "WorkChapterExNative": WorkChapterExNative;
            "WorkChapterMapNative": IMap<number, WorkChapterNative>;
            "PageTitleNative": PageTitleNative;
        }
        interface ClassNameSourceMap {
            "WorkChapterNative": IWorkChapter;
            "WorkChapterExNative": IWorkChapterEx;
            "WorkChapterMapNative": {[key:number] : WorkChapterNative };
            "PageTitleNative": IPageTitle;
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
            pageTitle: PageTitleNative | null;

            areUrlsInReadingListAsync(urls: string[]): WinJS.Promise<IMap<string, boolean>>;
        }

        function PropKeyToNum(key: PropertyKey): number | null {
            if (typeof key === "number") {
                return key;
            }
            else if (typeof key === "string") {
                let num = Number(key);
                if (num.toString() === key) return num;
            }
            return null;
        }

        function PropKeyToString(key: PropertyKey): string | null {
            if (typeof key === "number") {
                return key.toString();
            }
            else if (typeof key === "string") {
                return key;
            }
            return null;
        }

        function GetIMapProxy<V, I extends string | number>(map: IMap<I, V>, conv: (key: PropertyKey) => (I | null)): any {
            let proxy = new Proxy(map, {
                get: (oTarget, sKey) => {
                    let key = conv(sKey);
                    if (key === null) return undefined;
                    return oTarget.hasKey(key) ? oTarget.lookup(key) : undefined;
                },
                set: (oTarget, sKey, vValue) => {
                    let key = conv(sKey);
                    if (key === null) return false;
                    oTarget.insert(key, vValue);
                    return true;
                },
                deleteProperty: (oTarget, sKey) => {
                    let key = conv(sKey);
                    if (key === null) return false;
                    oTarget.remove(key);
                    return true;
                },
                enumerate: (oTarget) => {
                    let keys: PropertyKey[] = [];
                    for (let it = oTarget.first(); it.hasCurrent; it.moveNext()) {
                        keys.push(it.current.key);
                    }
                    return keys;
                },
                ownKeys: (oTarget) => {
                    let keys: PropertyKey[] = [];
                    for (let it = oTarget.first(); it.hasCurrent; it.moveNext()) {
                        keys.push(it.current.key);
                    }
                    return keys;
                },
                has: (oTarget, sKey) => {
                    let key = conv(sKey);
                    if (key === null) return false;
                    return oTarget.hasKey(key);
                },
                defineProperty: (oTarget, sKey, oDesc) => {
                    return false;
                },
                getOwnPropertyDescriptor: (oTarget, sKey) => {
                    let v = this.get(oTarget, sKey);
                    if (v === undefined) return undefined as any;

                    let res: PropertyDescriptor = {
                        value: v,
                        writable: true,
                        enumerable: true,
                        configurable: false
                    };
                    return res;
                }
            });

            return proxy;
        }

        function WrapIMapNum<V>(map: IMap<number, V>) {
            return GetIMapProxy(map, PropKeyToNum);
        }
        function WrapIMapString<V>(map: IMap<string, V>) {
            return GetIMapProxy(map, PropKeyToString);
        }
        function ToNative<K extends keyof ClassNameMap>(classname: K, source: ClassNameSourceMap[K]): ClassNameMap[K]{
            return Object.assign(Ao3TrackHelperUWP.createObject(classname), source);
        }
        function ToWorkChapterNative(source: IWorkChapter): ClassNameMap["WorkChapterNative"] {
            return ToNative("WorkChapterNative", source);
        }
        function ToWorkChapterExNative(source: IWorkChapterEx): ClassNameMap["WorkChapterExNative"] {
            return ToNative("WorkChapterExNative", source);
        }
        function ToPageTitleNative(source: IPageTitle): ClassNameMap["PageTitleNative"] {
            return ToNative("PageTitleNative", source);
        }
        function ToWorkChapterMapNative(source: { [key: number]: IWorkChapter }): ClassNameMap["WorkChapterMapNative"] {
            let m = Ao3TrackHelperUWP.createObject("WorkChapterMapNative");
            for (let key in source) {
                m.insert(key as any, ToWorkChapterNative(source[key]));
            }
            return m;
        }

        // Would be nice to autogenerate this nonsense by reflection
        let helperDef: Marshal.IHelperDef = {
            getWorkChaptersAsync: { args: { }, return: WrapIMapNum, promise: 1 },
            setWorkChapters: { args: { 0: ToWorkChapterMapNative } },
            onjumptolastlocationevent: { setter: true },
            jumpToLastLocationEnabled: { getter: true, setter: true },
            nextPage: { getter: true, setter: true },
            prevPage: { getter: true, setter: true },
            canGoBack: { getter: true },
            canGoForward: { getter: true },
            goBack: { args: {} },
            goForward: { args: {} },
            leftOffset: { getter: true, setter: true },
            showPrevPageIndicator: { getter: true, setter: true },
            showNextPageIndicator: { getter: true, setter: true },
            onalterfontsizeevent: { setter: true },
            fontSize: { getter: true, setter: true },
            showContextMenu: { args: {}, promise: 3 },
            addToReadingList: { args: {} },
            copyToClipboard: { args: {} },
            setCookies: { args: {} },
            currentLocation: { getter: true, setter: ToWorkChapterExNative },
            pageTitle: { getter: true, setter: ToPageTitleNative },
            areUrlsInReadingListAsync: { args: { }, return: WrapIMapString, promise: 1 },
        };

        for (let name in helperDef) {
            let def = helperDef[name];

            // It's a function!
            if (def.args !== undefined) {
                let func = ((Ao3TrackHelperUWP as any)[name] as Function).bind(Ao3TrackHelperUWP) as Function;

                if (def.return || Object.keys(def.args).length > 0 || def.promise !== undefined) {
                    let argconv = def.args;
                    let retconv = def.return || null;
                    (Ao3Track.Helper as any)[name] = function () {
                        let args : any[] = [].slice.call(arguments);
                        for (let i in argconv) {
                            args[i] = argconv[i](args[i]);
                        }
                        let promcb = (def.promise !== undefined) ? args.splice(def.promise,1)[0] : null;
                        let ret = func.apply(Ao3TrackHelperUWP, args);
                        if (promcb) 
                        {
                            let prom = ret as WinJS.Promise<any>;
                            prom.then((v) => {
                                if (retconv) v = retconv(v);
                                promcb(v);
                            });
                            return;
                        }
                        else 
                        {
                            if (retconv) ret = retconv(ret);
                            return ret;
                        }
                    };
                }
                else {
                    (Ao3Track.Helper as any)[name] = func;
                }
            }
            // It's a property
            else if (def.getter || def.setter) {
                let newprop: PropertyDescriptor = { enumerable: true };

                if (def.getter) {

                    if (typeof def.getter === "function") {
                        let getter = def.getter;
                        newprop.get = () => getter((Ao3TrackHelperUWP as any)[name]);
                    }
                    else {
                        newprop.get = () => (Ao3TrackHelperUWP as any)[name];
                    }
                }
                if (def.setter) {
                    if (typeof def.setter === "function") {
                        let setter = def.setter;
                        newprop.set = (v) => (Ao3TrackHelperUWP as any)[name] = setter(v);
                    }
                    else {
                        newprop.set = (v) => (Ao3TrackHelperUWP as any)[name] = v;
                    }
                }
                Object.defineProperty(Ao3Track.Helper, name, newprop);
            }
        }
    }
}

