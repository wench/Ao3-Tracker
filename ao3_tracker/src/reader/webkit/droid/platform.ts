// tslint:disable-next-line:no-var-keyword
var Ao3TrackHelperDroid: Ao3Track.Droid.IAo3TrackHelperDroid;

namespace Ao3Track {
    Helper = {} as Ao3Track.IAo3TrackHelper;

    export namespace Droid {
        type jsonNumberArray = string;
        type jsonStringArray = string;
        type jsonWorkChapEx = string;
        type jsonWorkChapList = string;
        type jsonWorkStringBoolList = string;
        type jsonPageTitle = string;
        type hCallback<T> = number;

        export class IAo3TrackHelperDroid {
            constructor()
            {
            }
            getWorkChaptersAsync(works: jsonNumberArray, callback: hCallback<jsonWorkChapList>): void { }
            setWorkChapters(workchapters: jsonWorkChapList): void { }

            set_onjumptolastlocationevent(callback: hCallback<boolean>): void{ }
            get_JumpToLastLocationEnabled(): boolean { return false; }
            set_JumpToLastLocationEnabled(value: boolean): void { }

            get_NextPage(): string  {return "";}
            set_NextPage(value: string): void {}

            get_PrevPage(): string { return ""; }
            set_PrevPage(value: string): void{}

            get_CanGoBack(): boolean { return false; }
            get_CanGoForward(): boolean { return false; }

            goBack(): void {}
            goForward(): void {}

            get_LeftOffset(): number { return 0; }
            set_LeftOffset(value: number): void {}

            get_Opacity(): number { return 0; }
            set_Opacity(value: number): void {}

            set_ShowPrevPageIndicator(value: boolean): void {}
            set_ShowNextPageIndicator(value: boolean): void {}

            set_onalterfontsizeevent(callback: hCallback<any>): void {}
            get_FontSize(): number { return 0; }
            set_FontSize(value: number): void {}

            showContextMenu(x: number, y: number, menuItems: jsonStringArray, callback: hCallback<string>): void {}
            addToReadingList(href: string): void {}
            copyToClipboard(str: string, type: string): void {}
            setCookies(cookies: string): void {}

            get_CurrentLocation(): jsonWorkChapEx|null { return null; }
            set_CurrentLocation(value: jsonWorkChapEx|null): void {}

            get_PageTitle(): jsonPageTitle|null { return null; }
            set_PageTitle(value: jsonPageTitle|null): void {}

            areUrlsInReadingListAsync(works: jsonStringArray, callback: hCallback<jsonWorkStringBoolList>): void { }            
        }

        // Would be nice to autogenerate this nonsense by reflection
        let helperDef: Marshal.IHelperDef = {
            getWorkChaptersAsync: { args: { 0: JSON.stringify, 1: Ao3TrackCallbacks.JsonCallback } },
            setWorkChapters: { args: { 0: JSON.stringify } },
            onjumptolastlocationevent: { setter: Ao3TrackCallbacks.Permanent },
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
            onalterfontsizeevent: { setter: Ao3TrackCallbacks.Permanent },
            fontSize: { getter: true, setter: true },
            showContextMenu: { args: { 2: JSON.stringify, 3: Ao3TrackCallbacks.Callback } },
            addToReadingList: { args: {} },
            copyToClipboard: { args: {} },
            setCookies: { args: {} },
            currentLocation: { getter: JSON.parse, setter: JSON.stringify },
            pageTitle: { getter: JSON.parse, setter: JSON.stringify },
            areUrlsInReadingListAsync: { args: { 0: JSON.stringify, 1: Ao3TrackCallbacks.JsonCallback } },
        };

        for (let name in helperDef) {
            let def = helperDef[name];

            // It's a function!
            if (def.args !== undefined) {
                let func = ((Ao3TrackHelperDroid as any)[name] as Function).bind(Ao3TrackHelperDroid) as Function;

                if (def.return || Object.keys(def.args).length > 0) {
                    let defarg = def.args;
                    let defret = def.return || null;
                    (Ao3Track.Helper as any)[name] = function () {
                        let args : any[] = [].slice.call(arguments);
                        for (let i in defarg) {
                            args[i] = defarg[i](args[i]);
                        }
                        let ret = func.apply(Ao3TrackHelperDroid, args);
                        if (defret) ret = defret(ret);
                        return ret;
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
                    let gname = "get_" + name;
                    
                    let gfunc = ((Ao3TrackHelperDroid as any)[gname] as Function).bind(Ao3TrackHelperDroid);
                    if (typeof def.getter === "function") {
                        let getter  = def.getter;
                        newprop.get = () => getter(gfunc());
                    }
                    else {                    
                        newprop.get = gfunc;
                    }
                }
                if (def.setter) {
                    let sname = "set_" + name;
                    let sfunc = ((Ao3TrackHelperDroid as any)[sname] as Function).bind(Ao3TrackHelperDroid);
                    if (typeof def.setter === "function") {
                        let setter  = def.setter;
                        newprop.set = (v) => sfunc(setter(v));
                    }
                    else {
                         newprop.set = sfunc;
                    }
                }
                Object.defineProperty(Ao3Track.Helper, name, newprop);                
            }
        }
    }
}

