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

// Text to speech? Text to speech!

// Mostly platform specific and handled in C# but we need to get the text from the webpage into the app which is what happens in here

namespace Ao3Track {
    if (work_chapter && work_chapter.length === 3)
    {
        Ao3Track.Helper.onrequestspeechtext = () => {
            let speechText : ISpeechText[] = [];

            // $chapter_text

            Ao3Track.Helper.setSpeechText(speechText);
        };
        
    }
}
