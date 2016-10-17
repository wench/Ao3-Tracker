/// <reference path="../typings/index.d.ts" />

declare function escape(str : string) : string;
declare function unescape(str : string) : string;
interface DirectoryEntry { }

type FormErrorList = { [key:string]:string; };

interface IWorkChapter {
    number: number;
    chapterid: number;
    location: number | null;
}
 
interface IWorkChapterTS extends IWorkChapter {
    timestamp: number;
}


interface IUserCreateData {
    username: string;
    password: string;
    email: string | null;
}

interface IUserLoginData {
    username: string;
    password: string;    
}

interface GetWorkChaptersMessage {
    type: 'GET';
    data: number[];
} 
type GetWorkChaptersMessageResponse = { [key:number]:IWorkChapter; };

interface SetWorkChaptersMessage {
    type: 'SET';
    data: { [key:number]:IWorkChapter; };
}
type SetWorkChaptersMessageResponse = never;

interface UserCreateMessage {
    type: 'USER_CREATE';
    data: IUserCreateData;
}
type UserCreateMessageResponse = FormErrorList;

interface UserLoginMessage {
    type: 'USER_LOGIN';
    data: IUserLoginData;
}
type UserLoginMessageResponse = FormErrorList;

interface DoSyncMessage {
    type: 'DO_SYNC';
}
type SetDoSyncMessageResponse = boolean;

type MessageType = GetWorkChaptersMessage|SetWorkChaptersMessage|UserCreateMessage|UserLoginMessage|DoSyncMessage;


