/// <reference path="../typings/globals/jquery/index.d.ts" />

declare function escape(str : string) : string;
declare function unescape(str : string) : string;
interface DirectoryEntry { }

type FormErrorList = { [key:string]:string; };

type timestamp = number;

interface IWorkChapter {
    number: number;
    chapterid: number;
    location: number | null;
    seq: number | null;
}
 interface IWorkChapterEx extends IWorkChapter {
    workid: number;
} 
interface IWorkChapterTS extends IWorkChapter {
    timestamp: timestamp;
}

declare namespace Ao3Track {
    export let GetWorkChapters : (works: number[], callback: (workchapters: { [key:number]:IWorkChapter }) => void) => void;
    export let SetWorkChapters : (workchapters: { [key: number]: IWorkChapter; }) => void;
    export let SetNextPage : (uri: string) => void;
    export let SetPrevPage : (uri: string) => void;
    export let DisableLastLocationJump : () => void;
    export let EnableLastLocationJump : (workid: number, lastloc: IWorkChapter) => void;
    export let SetCurrentLocation : (current : IWorkChapterEx) => void;    
    export let AreUrlsInReadingList : (urls: string[], callback: (result: { [key:string]:boolean})=> void) => void;
}
