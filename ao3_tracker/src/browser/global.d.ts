/// <reference path="../../typings/globals/chrome/index.d.ts" />

declare namespace Ao3Track {
    interface IUserCreateData {
        username: string;
        password: string;
        email: string | null;
    }

    interface IUserLoginData {
        username: string;
        password: string;
    }

    interface IReadingList {
        last_sync: timestamp;
        paths: { [key: string]: { uri: string; timestamp: timestamp } };
    }

    interface IServerReadingList {
        last_sync: timestamp;
        paths: { [key: string]: timestamp; };
    }

    interface GetWorkChaptersMessage {
        type: 'GET';
        data: number[];
    }
    type GetWorkChaptersMessageResponse = { [key: number]: IWorkChapter; };

    interface SetWorkChaptersMessage {
        type: 'SET';
        data: { [key: number]: IWorkChapter; };
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

    interface UserLogoutMessage {
        type: 'USER_LOGOUT';
    }
    type UserLogoutMessageResponse = boolean;

    interface UserNameMessage {
        type: 'USER_NAME';
    }
    type UserNameMessageResponse = boolean;

    interface DoSyncMessage {
        type: 'DO_SYNC';
    }
    type DoSyncMessageResponse = boolean;

    type MessageType = GetWorkChaptersMessage | SetWorkChaptersMessage | UserCreateMessage | UserLoginMessage | UserLogoutMessage | UserNameMessage | DoSyncMessage;

    interface IsInReadingListMessage {
        type: 'RL_ISINLIST';
        data: string[];
    }
    type IsInReadingListMessageResponse = { [key: string]: boolean; };

    type ReadingListMessageType = IsInReadingListMessage;

}