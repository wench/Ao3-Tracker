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

/// <reference path="../typings/globals/jquery/index.d.ts" />

interface DirectoryEntry { }

declare namespace Ao3Track {

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

    interface IUnitConvOptions {
        tempToC?: boolean;
        distToM?: boolean;
        volumeToM?: boolean;
        weightToM?: boolean;
    }

    interface IWorkDetails {
        savedLoc?: IWorkChapter,
        inReadingList?: boolean
    }

    export let GetWorkDetails : (works: number[],  callback: (details: { [key:number]:IWorkDetails }) => void, flags?: WorkDetailsFlags) => void;
    export let SetWorkChapters : (workchapters: { [key: number]: IWorkChapter; }) => void;
    export let SetNextPage : (uri: string) => void;
    export let SetPrevPage : (uri: string) => void;
    export let DisableLastLocationJump : () => void;
    export let EnableLastLocationJump : (workid: number, lastloc: IWorkChapter) => void;
    export let SetCurrentLocation : (current : IWorkChapterEx) => void;    
    export let GetUnitConvOptions : (callback: (result :IUnitConvOptions)=>void) => void;
    export let AreUrlsInReadingList : (urls: string[], callback: (result: { [key:string]:boolean})=> void) => void;
    export let ShouldFilterWork : (workid: number, authors: string[], tags: string[], series: number[], callback: (filter: string|null)=>void) => void;
}
