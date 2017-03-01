namespace Ao3Track {
    export namespace Droid {
        type jsonNumberArray = string;
        type jsonStringArray = string;
        type jsonWorkChapEx = string;
        type jsonWorkChapList = string;
        type jsonWorkStringBoolList = string;
        type jsonPageTitle = string;
        type hCallback<T> = number;

        export let helper = Ao3TrackHelperNative as {
            get_scriptsToInject(): string;
            get_cssToInject(): string;
            get_memberDef(): string;

            [key:string] : any;       
        };
    }
    Marshal.MarshalNativeHelper(Droid.helper.get_memberDef(), Droid.helper);
}

