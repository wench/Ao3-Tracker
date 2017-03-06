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
            get_helperDefJson(): string;

            [key:string] : any;       
        };
    }
    Marshal.MarshalNativeHelper(Droid.helper.get_helperDefJson(), Droid.helper);
}

