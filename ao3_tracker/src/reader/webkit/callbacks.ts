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
