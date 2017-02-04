type jsonNumberArray = string;
type jsonStringArray = string;
type jsonWorkChapEx = string;
type jsonWorkChapList = string;
type hCallback<T> = number;
var Ao3TrackHelper: Ao3Track.Webkit.IAo3TrackHelper;

namespace Ao3Track {
    export namespace Webkit {
        export interface IAo3TrackHelper {
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
        }

        export var Marshalled = {           
            getWorkChaptersAsync(works: number[], callback: (workchapters: GetWorkChaptersMessageResponse) => void): void {
                let hCallback = Ao3TrackCallbacks.Add(callback);
                Ao3TrackHelper.getWorkChaptersAsync(JSON.stringify(works),hCallback);
            },

            setWorkChapters(workchapters: { [key: number]: IWorkChapter; }): void {
                Ao3TrackHelper.setWorkChapters(JSON.stringify(workchapters));
            },

            set onjumptolastlocationevent(handler : ((pagejump : boolean) => void) | null)
            {
                if (handler === null) { Ao3TrackHelper.set_onjumptolastlocationevent(0); }
                else { Ao3TrackHelper.set_onjumptolastlocationevent(Ao3TrackCallbacks.AddPermanent(handler)); }
            },
            
            set onalterfontsizeevent(handler : ((ev:any) => void) | null)
            {
                if (handler === null) { Ao3TrackHelper.set_onalterfontsizeevent(0); }
                else { Ao3TrackHelper.set_onalterfontsizeevent(Ao3TrackCallbacks.AddPermanent(handler)); }
            },
            
            showContextMenu(x: number, y: number, menuItems: string[], callback: (selected: string | null)=>void): void {
                let hCallback = Ao3TrackCallbacks.Add(callback);
                Ao3TrackHelper.showContextMenu(x, y, JSON.stringify(menuItems), hCallback);                
            },

            get currentLocation() : IWorkChapterEx | null {
                let value = Ao3TrackHelper.get_CurrentLocation();
                if (value === null) { return null; }
                return JSON.parse(value); 
            },
            set currentLocation(value : IWorkChapterEx | null)  { 
                if (value === null) { Ao3TrackHelper.set_CurrentLocation(null); }
                else { Ao3TrackHelper.set_CurrentLocation(JSON.stringify(value));  }
            }
        };

        for(let name of Object.getOwnPropertyNames(Object.getPrototypeOf(Ao3TrackHelper)))
        {
            if (name.startsWith("get_") || name.startsWith("get_"))
            {
                let pname = name.substr(4);
                let mname = pname[0].toLowerCase() + pname.substr(1);
                
                if (Object.getOwnPropertyDescriptor(Marshalled,mname)) { continue; }

                let gname = "get_" + pname;
                let sname = "set_" + pname;

                let getter = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(Ao3TrackHelper),gname);
                let setter = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(Ao3TrackHelper),sname);
                
                let newprop : PropertyDescriptor = { enumerable : true };

                if (getter && typeof getter.value === "function")
                {
                    newprop.get = ()=>{
                        return (Ao3TrackHelper as any)[gname]();
                    };
                }
                if (setter && typeof setter.value === "function")
                {
                    newprop.set = (value: any)=>{
                        (Ao3TrackHelper as any)[sname](value);
                    };
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

                let prop = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(Ao3TrackHelper),name);
    
                if (typeof prop.value === "function")
                {
                    let newprop : PropertyDescriptor = { enumerable : prop.enumerable || false };
                    newprop.value =  function(){
                        prop.value.apply(Ao3TrackHelper, arguments);
                    };
                    Object.defineProperty(Marshalled,name,newprop);   
                }
            }            
        }
    }
    Helper = Ao3Track.Webkit.Marshalled as Ao3Track.IAo3TrackHelper;
}

