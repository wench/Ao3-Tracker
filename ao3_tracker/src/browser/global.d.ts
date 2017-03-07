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

    interface IMessageRequest<D,R,K extends keyof D & keyof R>
    {
        type: K,
        data: D[K],
        sendResponse: ((response: R[K]) => void);
    }
    interface IMessageRequestNoResponse<D, R extends {[P in K]: undefined}, K extends keyof D>
    {
        type: K,
        data: D[K],
        sendResponse: undefined;
    }

    interface CloudMessageData
    {
        'GET': number[];
        'SET': { [key: number]: IWorkChapter; };
        'USER_CREATE': IUserCreateData;
        'USER_LOGIN': IUserLoginData;
        'USER_LOGOUT': undefined;
        'USER_NAME' : undefined;
        'DO_SYNC' : undefined;
    }
    interface CloudMessageResponse
    {
        'GET': { [key: number]: IWorkChapter; };
        'SET': never;
        'USER_CREATE': FormErrorList|null;
        'USER_LOGIN': FormErrorList|null;
        'USER_LOGOUT': boolean;
        'USER_NAME' : string;
        'DO_SYNC' : boolean;
    }
    
    type ICloudMessageRequest<K extends keyof CloudMessageData & keyof CloudMessageResponse> = IMessageRequest<CloudMessageData,CloudMessageResponse,K>;        
    type CloudMessageRequest = ICloudMessageRequest<'GET'> | ICloudMessageRequest<'USER_CREATE'> | ICloudMessageRequest<'USER_LOGIN'> | 
                                ICloudMessageRequest<'USER_LOGOUT'> | ICloudMessageRequest<'USER_NAME'> | ICloudMessageRequest<'DO_SYNC'> |
                                IMessageRequestNoResponse<CloudMessageData,CloudMessageResponse,'SET'>;

    interface ReadingListMessageData
    {
        'RL_ISINLIST': string[];
    }
    interface ReadingListMessageResponse
    {
        'RL_ISINLIST': { [key: string]: boolean; };
    }
    type IReadingListMessageRequest<K extends keyof ReadingListMessageData & keyof ReadingListMessageResponse> = IMessageRequest<ReadingListMessageData,ReadingListMessageResponse,K>;        
    type ReadingListMessageRequest = IReadingListMessageRequest<'RL_ISINLIST'>;
    
    interface MessageData extends CloudMessageData, ReadingListMessageData
    {
    }    
    
    interface MessageResponse extends CloudMessageResponse, ReadingListMessageResponse
    {
    }

    type MessageRequest = CloudMessageRequest | ReadingListMessageRequest;

    export let sendMessage : (request: MessageRequest)=>void;
}
