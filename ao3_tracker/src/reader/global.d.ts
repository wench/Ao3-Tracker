declare namespace Ao3Track {
       
    export interface IPageTitle
    {
        title: string;
        chapter?: string | null;
        chaptername?: string | null;
        authors?: string[]  | null;
        fandoms?: string[] | null;
        primarytag?: string | null;
    }    

    export interface IAo3TrackHelperMethods {
        getWorkChaptersAsync(works: number[], callback: (workchapters: { [key:number]:IWorkChapter }) => void) : void;
        setWorkChapters(workchapters: { [key: number]: IWorkChapter; }): void;

        goBack(): void;
        goForward(): void;

        showContextMenu(x: number, y: number, menuItems: string[], callback: (selected: string | null)=>void): void;
        hideContextMenu() : void;
        
        copyToClipboard(str: string, type: string): void;
        setCookies(cookies: string): void;

        addToReadingList(href: string): void;
        areUrlsInReadingListAsync(urls: string[], callback: (result: { [key:string]:boolean})=> void) : void;     

        startWebViewDragAccelerate(velocity: number) : void;
        stopWebViewDragAccelerate(): void;
    }

    export interface IAo3TrackHelperProperties {
        onjumptolastlocationevent: ((pagejump: boolean) => void) | null;
        onalterfontsizeevent: ((ev: number) => void) | null;
        
        nextPage: string;
        prevPage: string;
        canGoBack: boolean;
        canGoForward: boolean;

        deviceWidth: number;
        leftOffset: number;

        showPrevPageIndicator: number;
        showNextPageIndicator: number;

        currentLocation: IWorkChapterEx | null;
        pageTitle : IPageTitle | null;
    }

    export interface IAo3TrackHelper extends IAo3TrackHelperMethods, IAo3TrackHelperProperties {
    }

    export var Helper : Ao3Track.IAo3TrackHelper;
}

declare var Ao3TrackHelperNative : any;
