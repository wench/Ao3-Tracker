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

interface MessageHandler
{
    postMessage: (message: string) => void;
}

interface MessageHandlers
{
    ao3track: MessageHandler;
}

interface WebKit
{
    messageHandlers: MessageHandlers;
}

interface Window
{
    webkit: WebKit;
}

namespace Ao3Track {
    export namespace Messaging {
        notify = (json: string) => { 
            window.webkit.messageHandlers.ao3track.postMessage(json);
        };
    }    
}
