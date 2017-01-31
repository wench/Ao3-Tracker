namespace Ao3Track {
    export interface IAo3TrackHelper {
        getWorkChaptersAsync(works: number[], callback: (workchapters: GetWorkChaptersMessageResponse) => void) : void;

        setWorkChapters(workchapters: { [key: number]: IWorkChapter; }): void;

        onjumptolastlocationevent: ((pagejump: boolean) => void) | null;

        jumpToLastLocationEnabled: boolean;

        nextPage: string;
        prevPage: string;
        canGoBack: boolean;
        canGoForward: boolean;
        goBack(): void;
        goForward(): void;
        leftOffset: number;
        opacity: number;
        showPrevPageIndicator: boolean;
        showNextPageIndicator: boolean;

        onalterfontsizeevent: ((ev: any) => void) | null;
        fontSize: number;

        showContextMenu(x: number, y: number, menuItems: string[], callback: (selected: string | null)=>void): void;
        addToReadingList(href: string): void;
        copyToClipboard(str: string, type: string): void;
        setCookies(cookies: string): void;

        currentLocation: IWorkChapterEx | null;
    }

    export declare var Helper : Ao3Track.IAo3TrackHelper;
}
