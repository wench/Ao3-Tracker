namespace Ao3TrackCallbacks {
    type CallbackFuncType = ((arg:any)=>void) | (()=>void);

    interface Callback {
        func:  CallbackFuncType;
        permanent: boolean;
        json : boolean;
    }

    let storage : {[key:number]: Callback} = { };
    let counter = 0;

    export function Add(func: CallbackFuncType, json: boolean = true) : number
    {
        storage[++counter] = {
            func: func,
            permanent: false,
            json: json
        };
        return counter;
    }

    export function AddPermanent(func: CallbackFuncType, json: boolean = true) : number
    {
        storage[++counter] = {
            func: func,
            permanent: true,
            json: json
        };
        return counter;
    } 

    export function Call(handle: number, arg: any) : void
    {
        if (handle in storage)
        {   
            let callback = storage[handle];
            (callback.func as (arg:any)=>void)(callback.json ? JSON.parse(arg) : arg);
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
