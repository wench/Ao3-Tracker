declare namespace Ao3TrackHelper {
    type jsonNumberArray = string;
    type jsonStringArray = string;
    type jsonWorkChapEx = string;
    type jsonWorkChapList = string;
    type hCallback<T> = number

    function get_ScriptsToInject() : jsonStringArray;
    function get_CssToInject() : jsonStringArray;

    function set_JumpToLastLocationCallback(callback : hCallback<boolean>) : void;
    function get_JumpToLastLocationEnabled() : boolean; 
    function set_JumpToLastLocationEnabled(value: boolean) : void; 

    function set_AlterFontSizeCallback(callback : hCallback<void>) : void;
    function get_FontSize() : number;
    function set_FontSize(value: number) : void;

    function getWorkChaptersAsync(works: jsonNumberArray, callback : hCallback<jsonWorkChapList>) : void;
    function setWorkChapters(workchapters: jsonWorkChapList): void;
  
    function showContextMenu(x: number, y: number, menuItems: jsonStringArray, callback : hCallback<string>) : void;
    function addToReadingList(href: string) : void;
    function copyToClipboard(str: string, type:string) : void; 
    function setCookies(cookies: string) : void;

    function get_NextPage() : string;
    function set_NextPage(value : string) : void;

    function get_PrevPage() : string;
    function set_PrevPage(value : string) : void;

    function get_CanGoBack() :boolean;
    function get_CanGoForward() :boolean;

    function goBack() : void;
    function goForward() : void;

    function get_LeftOffset(): number;
    function set_LeftOffset(value : number) : void;

    function get_Opacity() : number;
    function set_Opacity(value : number) : void;

    function set_ShowPrevPageIndicator(value : boolean) : void;
    function set_ShowNextPageIndicator(value : boolean) : void;

    function get_CurrentLocation() : jsonWorkChapEx;
    function set_CurrentLocation(value: jsonWorkChapEx) : void;

}