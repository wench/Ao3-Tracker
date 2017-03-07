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

/// <reference path="../../../typings/globals/winjs/index.d.ts" />

namespace Ao3Track {
    export namespace UWP {
        export interface Native {
            native: never;
        }

        export interface IKeyValuePair<K, V> {
            key: K;
            value: V;
        }

        export interface IIterator<T> {
            readonly current: T;
            getMany: () => any;
            readonly hasCurrent: boolean;
            moveNext: () => boolean;
        }
        export interface IIterable<T> {
            first: () => IIterator<T>;
        }

        export interface IMapView<K, V> extends IIterable<IKeyValuePair<K, V>> {
            hasKey: (key: K) => boolean;
            lookup: (key: K) => V;
            size: number;
            split: () => { first: IMapView<K, V>; second: IMapView<K, V> };
        }

        export interface IMap<K, V> extends IIterable<IKeyValuePair<K, V>> {
            clear: () => void;
            getView: () => IMapView<K, V>;
            hasKey: (key: K) => boolean;
            insert: (key: K, value: V) => boolean;
            lookup: (key: K) => V;
            remove: (key: K) => void;
            size: number;
        }

        export interface ClassNameMap {
            "WorkChapterNative": IWorkChapter;
            "WorkChapterExNative": IWorkChapterEx;
            "WorkChapterMapNative": { [key: number]: IWorkChapter } | IMap<number,IWorkChapter & Native>;
            "PageTitleNative": IPageTitle;
        }

        export let helper = Ao3TrackHelperNative as  {
            helperDefJson: string;

            // Create native objects to pass back           
            createObject<K extends keyof ClassNameMap>(classname: K): ClassNameMap[K] & Native;

            [key:string] : any;
        }

        export function PropKeyToNum(key: PropertyKey): number | null {
            if (typeof key === "number") {
                return key;
            }
            else if (typeof key === "string") {
                let num = Number(key);
                if (num.toString() === key) return num;
            }
            return null;
        }

        export function PropKeyToString(key: PropertyKey): string | null {
            if (typeof key === "number") {
                return key.toString();
            }
            else if (typeof key === "string") {
                return key;
            }
            return null;
        }

        export function GetIMapProxy<V, I extends string | number>(map: IMap<I, V>, conv: (key: PropertyKey) => (I | null)): any {
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

        export function ToNative<K extends keyof ClassNameMap>(classname: K, source: ClassNameMap[K]): ClassNameMap[K] & UWP.Native{
            return Object.assign(helper.createObject(classname), source);
        }
    }

    export namespace Marshal {
        export namespace Converters {

            export function ToWorkChapterNative(source: IWorkChapter) {
                return UWP.ToNative("WorkChapterNative", source);
            }
            export function ToWorkChapterExNative(source: IWorkChapterEx) {
                return UWP.ToNative("WorkChapterExNative", source);
            }
            export function ToPageTitleNative(source: IPageTitle) {
                return UWP.ToNative("PageTitleNative", source);
            }
            export function ToWorkChapterMapNative(source: { [key: number]: IWorkChapter }) {
                let m = UWP.helper.createObject("WorkChapterMapNative") as UWP.IMap<number,IWorkChapter & UWP.Native>;
                for (let key in source) {
                    m.insert(key as any, ToWorkChapterNative(source[key]));
                }
                return m;
            }

            export function WrapIMapNum<V>(map: UWP.IMap<number, V>) {
                return UWP.GetIMapProxy(map, UWP.PropKeyToNum);
            };
            export function WrapIMapString<V>(map: UWP.IMap<string, V>) {
                return UWP.GetIMapProxy(map, UWP.PropKeyToString);
            };

        }
    }

    Marshal.MarshalNativeHelper(UWP.helper.helperDefJson, UWP.helper);
}

