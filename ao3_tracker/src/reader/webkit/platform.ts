var Ao3TrackHelperWebkit: Ao3Track.Webkit.IAo3TrackHelperWebkit;

namespace Ao3Track {
    export namespace Webkit {
        type jsonNumberArray = string;
        type jsonStringArray = string;
        type jsonWorkChapEx = string;
        type jsonWorkChapList = string;
        type jsonPageTitle = string;
        type hCallback<T> = number;
        
        export interface IAo3TrackHelperWebkit {
            getWorkChaptersAsync(works: jsonNumberArray, callback: hCallback<jsonWorkChapList>): void;
            setWorkChapters(workchapters: jsonWorkChapList): void;

            set_onjumptolastlocationevent(callback: hCallback<boolean>): void;
            get_JumpToLastLocationEnabled(): boolean;
            set_JumpToLastLocationEnabled(value: boolean): void;

            get_NextPage(): string;
            set_NextPage(value: string): void;

            get_PrevPage(): string;
            set_PrevPage(value: string): void;

            get_CanGoBack(): boolean;
            get_CanGoForward(): boolean;

            goBack(): void;
            goForward(): void;

            get_LeftOffset(): number;
            set_LeftOffset(value: number): void;

            get_Opacity(): number;
            set_Opacity(value: number): void;

            set_ShowPrevPageIndicator(value: boolean): void;
            set_ShowNextPageIndicator(value: boolean): void;

            set_onalterfontsizeevent(callback: hCallback<any>): void;
            get_FontSize(): number;
            set_FontSize(value: number): void;

            showContextMenu(x: number, y: number, menuItems: jsonStringArray, callback: hCallback<string>): void;
            addToReadingList(href: string): void;
            copyToClipboard(str: string, type: string): void;
            setCookies(cookies: string): void;

            get_CurrentLocation(): jsonWorkChapEx|null;
            set_CurrentLocation(value: jsonWorkChapEx|null): void;

            get_PageTitle(): jsonPageTitle|null;
            set_PageTitle(value: jsonPageTitle|null): void;            
        }

        export var Marshalled = {           
            getWorkChaptersAsync(works: number[], callback: (workchapters: GetWorkChaptersMessageResponse) => void): void {
                let hCallback = Ao3TrackCallbacks.Add(callback);
                Ao3TrackHelperWebkit.getWorkChaptersAsync(JSON.stringify(works),hCallback);
            },

            setWorkChapters(workchapters: { [key: number]: IWorkChapter; }): void {
                Ao3TrackHelperWebkit.setWorkChapters(JSON.stringify(workchapters));
            },

            set onjumptolastlocationevent(handler : ((pagejump : boolean) => void) | null)
            {
                if (handler === null) { Ao3TrackHelperWebkit.set_onjumptolastlocationevent(0); }
                else { Ao3TrackHelperWebkit.set_onjumptolastlocationevent(Ao3TrackCallbacks.AddPermanent(handler)); }
            },
            
            set onalterfontsizeevent(handler : ((ev:any) => void) | null)
            {
                if (handler === null) { Ao3TrackHelperWebkit.set_onalterfontsizeevent(0); }
                else { Ao3TrackHelperWebkit.set_onalterfontsizeevent(Ao3TrackCallbacks.AddPermanent(handler)); }
            },
            
            showContextMenu(x: number, y: number, menuItems: string[], callback: (selected: string | null)=>void): void {
                let hCallback = Ao3TrackCallbacks.Add(callback);
                Ao3TrackHelperWebkit.showContextMenu(x, y, JSON.stringify(menuItems), hCallback);                
            },

            get currentLocation() : IWorkChapterEx | null {
                let value = Ao3TrackHelperWebkit.get_CurrentLocation();
                if (value === null) { return null; }
                return JSON.parse(value); 
            },
            set currentLocation(value : IWorkChapterEx | null)  { 
                if (value === null) { Ao3TrackHelperWebkit.set_CurrentLocation(null); }
                else { Ao3TrackHelperWebkit.set_CurrentLocation(JSON.stringify(value));  }
            },

            get pageTitle(): IPageTitle | null {
                let value = Ao3TrackHelperWebkit.get_PageTitle();
                if (value === null) { return null; }
                return JSON.parse(value); 
                },
            set pageTitle(value :IPageTitle | null) { 
                if (value === null) { Ao3TrackHelperWebkit.set_PageTitle(null); }
                else { Ao3TrackHelperWebkit.set_PageTitle(JSON.stringify(value)); }             
            }      
        };

        for(let name of Object.getOwnPropertyNames(Object.getPrototypeOf(Ao3TrackHelperWebkit)))
        {
            if (name.startsWith("get_") || name.startsWith("get_"))
            {
                let pname = name.substr(4);
                let mname = pname[0].toLowerCase() + pname.substr(1);
                
                if (Object.getOwnPropertyDescriptor(Marshalled,mname)) { continue; }

                let gname = "get_" + pname;
                let sname = "set_" + pname;

                let getter = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(Ao3TrackHelperWebkit),gname);
                let setter = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(Ao3TrackHelperWebkit),sname);
                
                let newprop : PropertyDescriptor = { enumerable : true };

                if (getter && typeof getter.value === "function")
                {
                    newprop.get = getter.value.bind(Ao3TrackHelperWebkit);
                }
                if (setter && typeof setter.value === "function")
                {
                    newprop.set = setter.value.bind(Ao3TrackHelperWebkit);
                }
                if (!newprop.get && !newprop.set)
                {
                    continue;
                }
                
                Object.defineProperty(Marshalled,mname,newprop);   
            }
            else
            {
                if (Object.getOwnPropertyDescriptor(Marshalled,name)) { continue; }

                let prop = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(Ao3TrackHelperWebkit),name);
    
                if (typeof prop.value === "function")
                {
                    let newprop : PropertyDescriptor = { enumerable : prop.enumerable || false };
                    newprop.value = prop.value.bind(Ao3TrackHelperWebkit);
                    Object.defineProperty(Marshalled,name,newprop);   
                }
            }            
        }
    }
    Helper = Ao3Track.Webkit.Marshalled as any as Ao3Track.IAo3TrackHelper;
}

