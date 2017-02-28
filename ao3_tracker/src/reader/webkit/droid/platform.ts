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

        let helperDef: Marshal.IHelperDef = {
            getWorkChaptersAsync: { args: { 0: Marshal.Converters.ToJSON, 1: Marshal.Converters.Callback } },
            setWorkChapters: { args: { 0: Marshal.Converters.ToJSON } },
            onjumptolastlocationevent: { setter: Marshal.Converters.Event },
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
            onalterfontsizeevent: { setter: Marshal.Converters.Event },
            fontSize: { getter: true, setter: true },
            showContextMenu: { args: { 2: Marshal.Converters.ToJSON, 3: Marshal.Converters.Callback } },
            addToReadingList: { args: {} },
            copyToClipboard: { args: {} },
            setCookies: { args: {} },
            currentLocation: { getter: Marshal.Converters.FromJSON, setter: Marshal.Converters.ToJSON },
            pageTitle: { getter: Marshal.Converters.FromJSON, setter: Marshal.Converters.ToJSON },
            areUrlsInReadingListAsync: { args: { 0: Marshal.Converters.ToJSON, 1: Marshal.Converters.Callback } },
        };
    }
    Marshal.MarshalNativeHelper(Droid.helper.get_memberDef(), Droid.helper);
}

