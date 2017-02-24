// tslint:disable-next-line:no-var-keyword
var Ao3TrackHelperWebkit: Ao3Track.Webkit.IAo3TrackHelperWebkit;

interface ObjectConstructor
{
    assign(target : any, varArgs : any) : any;
}

if (typeof Object.assign !== 'function') {
  Object.assign = function(target : any, varArgs : any) { // .length of function is 2
    'use strict';
    if (target === null) { // TypeError if undefined or null
      throw new TypeError('Cannot convert undefined or null to object');
    }

    let to = Object(target);

    for (let index = 1; index < arguments.length; index++) {
      let nextSource = arguments[index];

      if (nextSource !== null) { // Skip over if undefined or null
        for (let nextKey in nextSource) {
          // Avoid bugs when hasOwnProperty is shadowed
          if (Object.prototype.hasOwnProperty.call(nextSource, nextKey)) {
            to[nextKey] = nextSource[nextKey];
          }
        }
      }
    }
    return to;
  };
}

interface String
{
    endsWith(searchString: string, position?: number) : boolean;
    startsWith(searchString: string, position?: number) : boolean;
}

if (!String.prototype.endsWith) {
  String.prototype.endsWith = function(searchString: string, position?: number) {
      let subjectString : string = this.toString();
      if (typeof position !== 'number' || !isFinite(position) || Math.floor(position) !== position || position > subjectString.length) {
        position = subjectString.length;
      }
      position -= searchString.length;
      let lastIndex = subjectString.lastIndexOf(searchString, position);
      return lastIndex !== -1 && lastIndex === position;
  };
}

if (!String.prototype.startsWith) {
    String.prototype.startsWith = function(searchString: string, position?: number){
      position = position || 0;
      return this.substr(position, searchString.length) === searchString;
  };
}
namespace Ao3Track {
    export namespace Webkit {
        type jsonNumberArray = string;
        type jsonStringArray = string;
        type jsonWorkChapEx = string;
        type jsonWorkChapList = string;
        type jsonPageTitle = string;
        type hCallback<T> = number;
        
        export class IAo3TrackHelperWebkit {
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
        }

        export let Marshalled = {           
            getWorkChaptersAsync(works: number[], callback: (workchapters: { [key:number]:IWorkChapter }) => void): void {
                let hCallback = Ao3TrackCallbacks.Add(callback);
                Ao3TrackHelperWebkit.getWorkChaptersAsync(JSON.stringify(works),hCallback);
            },

            setWorkChapters(workchapters: { [key: number]: IWorkChapter; }): void {
                Ao3TrackHelperWebkit.setWorkChapters(JSON.stringify(workchapters));
            },

            set onjumptolastlocationevent(handler : ((pagejump : boolean) => void) | null)
            {
                if (handler === null) { Ao3TrackHelperWebkit.set_onjumptolastlocationevent(0); }
                else { Ao3TrackHelperWebkit.set_onjumptolastlocationevent(Ao3TrackCallbacks.AddPermanent(handler, false)); }
            },
            
            set onalterfontsizeevent(handler : ((ev:any) => void) | null)
            {
                if (handler === null) { Ao3TrackHelperWebkit.set_onalterfontsizeevent(0); }
                else { Ao3TrackHelperWebkit.set_onalterfontsizeevent(Ao3TrackCallbacks.AddPermanent(handler, false)); }
            },
            
            showContextMenu(x: number, y: number, menuItems: string[], callback: (selected: string | null)=>void): void {
                let hCallback = Ao3TrackCallbacks.Add(callback, false);
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
        let helperobj = new IAo3TrackHelperWebkit();

        for(let name of Object.getOwnPropertyNames(Object.getPrototypeOf(helperobj)))
        {
            if (name === "constructor" || name.startsWith("_"))
                continue;
            
            if (name.startsWith("get_") || name.startsWith("set_"))
            {
                let pname = name.substr(4);
                let mname = pname[0].toLowerCase() + pname.substr(1);
                
                if (Object.getOwnPropertyDescriptor(Marshalled,mname)) { continue; }

                let gname = "get_" + pname;
                let sname = "set_" + pname;

                let getter = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(helperobj),gname);
                let setter = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(helperobj),sname);
                
                let newprop : PropertyDescriptor = { enumerable : true };

                if (getter && typeof getter.value === "function")
                {
                    let gfunc  = (Ao3TrackHelperWebkit as any)[gname] as Function;
                    newprop.get = gfunc.bind(Ao3TrackHelperWebkit);
                }
                if (setter && typeof setter.value === "function")
                {
                    let sfunc  = (Ao3TrackHelperWebkit as any)[sname] as Function;
                    newprop.set = sfunc.bind(Ao3TrackHelperWebkit);
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

                let prop = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(helperobj),name);
    
                if (typeof prop.value === "function")
                {
                    let newprop : PropertyDescriptor = { enumerable : prop.enumerable || false };
                    let func  = (Ao3TrackHelperWebkit as any)[name] as Function;
                    newprop.value = func.bind(Ao3TrackHelperWebkit);
                    Object.defineProperty(Marshalled,name,newprop);   
                }
            }            
        }
    }
    Helper = Ao3Track.Webkit.Marshalled as any as Ao3Track.IAo3TrackHelper;
}

