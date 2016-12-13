namespace Ao3TrackCallbacks {
    type CallbackFuncType = ((arg:any)=>void) | (()=>void);

    interface Callback {
        func:  CallbackFuncType;
        permanent: boolean;
    }

    let storage : {[key:number]: Callback} = { };
    let counter = 0;

    export function Add(func: CallbackFuncType) : number
    {
        storage[++counter] = {
            func: func,
            permanent: false
        };
        return counter;
    }

    export function AddPermanent(func: CallbackFuncType) : number
    {
        storage[++counter] = {
            func: func,
            permanent: true
        };
        return counter;
    } 

    export function Call(handle: number, arg: string) : void
    {
        if (handle in storage)
        {   
            let callback = storage[handle];
            (callback.func as (arg:any)=>void)(JSON.parse(arg));
            if (!callback.permanent) {
                delete storage[handle];
            }
        }
    }
    
    export function CallVoid(handle: number) : void
    {
        if (handle in storage)
        {   
            let callback = storage[handle];
            (callback.func as ()=>void)();
            if (!callback.permanent) {
                delete storage[handle];
            }
        }
    }
}
