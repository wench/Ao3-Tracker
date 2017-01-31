type jsonNumberArray = string;
type jsonStringArray = string;
type jsonWorkChapEx = string;
type jsonWorkChapList = string;
type hCallback<T> = number;

export interface IAo3TrackHelper {

    get_ScriptsToInject() : jsonStringArray;
    get_CssToInject() : jsonStringArray;

    set_JumpToLastLocationCallback(callback : hCallback<boolean>) : void;
    get_JumpToLastLocationEnabled() : boolean; 
    set_JumpToLastLocationEnabled(value: boolean) : void; 

    set_AlterFontSizeCallback(callback : hCallback<void>) : void;
    get_FontSize() : number;
    set_FontSize(value: number) : void;

    getWorkChaptersAsync(works: jsonNumberArray, callback : hCallback<jsonWorkChapList>) : void;
    setWorkChapters(workchapters: jsonWorkChapList): void;
  
    showContextMenu(x: number, y: number, menuItems: jsonStringArray, callback : hCallback<string>) : void;
    addToReadingList(href: string) : void;
    copyToClipboard(str: string, type:string) : void; 
    setCookies(cookies: string) : void;

    get_NextPage() : string;
    set_NextPage(value : string) : void;

    get_PrevPage() : string;
    set_PrevPage(value : string) : void;

    get_CanGoBack() :boolean;
    get_CanGoForward() :boolean;

    goBack() : void;
    goForward() : void;

    get_LeftOffset(): number;
    set_LeftOffset(value : number) : void;

    get_Opacity() : number;
    set_Opacity(value : number) : void;

    set_ShowPrevPageIndicator(value : boolean) : void;
    set_ShowNextPageIndicator(value : boolean) : void;

    get_CurrentLocation() : jsonWorkChapEx;
    set_CurrentLocation(value: jsonWorkChapEx) : void;    
}

declare var Ao3TrackHelperBase : IAo3TrackHelper;

declare var Ao3TrackHelperProperties : { };

export var Ao3TrackHelper = Object.create(Ao3TrackHelperBase,Ao3TrackHelperProperties);
