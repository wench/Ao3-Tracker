/*
Copyright 2017 Alexis Ryan

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

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

        showContextMenu(x: number, y: number, url: string, innerHtml: string): void;
        
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
        swipeCanGoBack: boolean;
        swipeCanGoForward: boolean;

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
