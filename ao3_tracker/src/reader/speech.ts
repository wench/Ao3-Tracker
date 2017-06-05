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

    export interface ISpeechTextChapter
    {
        chapterId: number;
        chapterNum: number;
        title?: string;
        summary?: string;
        notesBegin?: string;
        paragraphs: string[];
        notesEnd?: string;
    }

    export interface ISpeechText
    {
        workId: number;
        title: string;
        authors: string[];
        summary?: string;
        notesBegin?: string;
        chapters: ISpeechTextChapter[];
        notesEnd?: string;
    }
    
    if (work_chapter && work_chapter.length === 3)
    {
        Ao3Track.Helper.onrequestspeechtext = (ev) => {
            let $workskin = $('#workskin'); 

            let speechText : ISpeechText = {
                workId : Ao3Track.workid,
                chapters: [],
                title: $workskin.find('> .preface:first > .title').text().trim(),
                authors: []
            };

            let $summary = $workskin.find('> .preface:first > .summary > .userstuff');
            let $notesBegin = $workskin.find('> .preface:first > .notes:not(.end) > .userstuff');
            let $notesEnd = $workskin.find('> .preface:last > .notes.end > .userstuff');

            let $authors = $workskin.find('> .preface:first > .byline > a[rel=author]');
            $authors.each((index,elem)=>{
                let a = elem as HTMLAnchorElement;
                if (a.innerText) { speechText.authors.push(a.innerText.trim()); }
            });                

            if ($summary.length !== 0) speechText.summary = $summary.html().trim();
            if ($notesBegin.length !== 0) speechText.notesBegin = $notesBegin.html().trim();
            if ($notesEnd.length !== 0) speechText.notesEnd = $notesEnd.html().trim();

            $chapter_text.each((index,elem) => {
                let $chapter_text = $(elem);
                let $chapter = $chapter_text.parent();
                let data: { num: number, id: number } = $chapter_text.data('ao3t');

                let speechTextChap : ISpeechTextChapter = {
                    chapterId: data.id,
                    chapterNum: data.num,
                    paragraphs: []
                };

                let $title = $chapter.find('> .preface:first > .title');
                let $summary = $chapter.find('> .preface:first > .summary > .userstuff');
                let $notesBegin = $chapter.find('> .preface:first > .notes:not(.end) > .userstuff');
                let $notesEnd = $chapter.find('> .preface:last > .notes.end > .userstuff');
                
                if ($title.length !== 0) speechTextChap.title = $title.text().trim();
                if ($summary.length !== 0) speechTextChap.summary = $summary.html().trim();
                if ($notesBegin.length !== 0) speechTextChap.notesBegin = $notesBegin.html().trim();
                if ($notesEnd.length !== 0) speechTextChap.notesEnd = $notesEnd.html().trim();

                $chapter_text.children().not('h3.landmark.heading').each((index, elem) => {
                    speechTextChap.paragraphs.push(elem.outerHTML.trim());
                });

                speechText.chapters.push(speechTextChap);
            });

            Ao3Track.Helper.setSpeechText(speechText);
        };
        
    }
}
