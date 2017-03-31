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
    export namespace Callbacks {
        export type CallbackFuncType = ((arg: any) => void) | (() => void);

        interface Callback {
            func: CallbackFuncType;
            permanent: boolean;
        }

        let storage: { [key: number]: Callback } = {};
        let counter = 0;

        export function Add(func: CallbackFuncType | null): number {
            if (func === null) return 0;
            storage[++counter] = {
                func: func,
                permanent: false,
            };
            return counter;
        }

        export function AddPermanent(func: CallbackFuncType | null): number {
            if (func === null) return 0;
            storage[++counter] = {
                func: func,
                permanent: true,
            };
            return counter;
        }

        export function Call(handle: number, arg: any): void {
            if (handle in storage) {
                let callback = storage[handle];
                (callback.func as (arg: any) => void)(arg);
                if (!callback.permanent) {
                    delete storage[handle];
                }
            }
        }

        export function CallVoid(handle: number): void {
            if (handle in storage) {
                let callback = storage[handle];
                (callback.func as () => void)();
                if (!callback.permanent) {
                    delete storage[handle];
                }
            }
        }
    }

    export namespace Marshal {
        export namespace Converters
        {
            export function Callback(func: Callbacks.CallbackFuncType|null) : number 
            {
                return Callbacks.Add(func);
            }
            export function Event(func: Callbacks.CallbackFuncType|null) : number {
                return Callbacks.AddPermanent(func);
            }
        }
    }
    
}
