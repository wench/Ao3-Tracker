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

namespace Ao3Track {

    const $ = jQuery;

    // Enable swipe left to go to next page
    let $next = $('head link[rel=next], .chapter.next > a, :not(#comments_placeholder) .pagination > .next > a, .series > a.next, .series a:contains(\xBB), .navigation a:contains(Next)');
    if ($next.length > 0) { SetNextPage(($next[0] as HTMLAnchorElement).href); }

    let $prev = $('.head link[rel=prev], .chapter.previous > a, :not(#comments_placeholder) .pagination > .previous > a, .series > a.previous, .series a:contains(\xAB), .navigation a:contains(Prev)');
    if ($prev.length > 0) { SetPrevPage(($prev[0] as HTMLAnchorElement).href); }

    const LOC_PARA_MULTIPLIER = 1000000000;
    const LOC_PARA_BOTTOM = 500000000;
    const LOC_PARA_FRAC_MULTIPLER = 479001600;

    let $feedback_actions = $('#feedback > .actions');
    let $chapter_text = $();

    let jumpnow : IWorkChapter | boolean = false;
    let jump = window.location.hash.split(":");
    if (jump.length >= 1 && jump[0] === "#ao3tjump") {
        if (jump.length === 1) {
            jumpnow = true;
        } else if (jump.length === 3 || jump.length === 4) {
            jumpnow = {
                number: parseInt(jump[1]),
                chapterid: parseInt(jump[2]),
                location: jump.length===4?parseInt(jump[3]):null,
                seq: null
            };       
            if (isNaN(jumpnow.number) || isNaN(jumpnow.chapterid) || (jumpnow.location !== null && isNaN(jumpnow.location)) )
                jumpnow = false;
        }
    }
    if (jumpnow !== false) {
        history.replaceState({}, document.title, window.location.href.substr(0, window.location.href.indexOf('#')));
    }

    let search_params : { [key:string]: string[] } =  { };

    let s = window.location.search;
    if (s.startsWith("?")) s = s.substr(1);
    decodeURIComponent(s).split("&").forEach((value,index,array)=>{
        let pair = value.split("=",2);
        let a = search_params[pair[0]] || [];
        a.push(pair[1] || '');
        search_params[pair[0]] = a;
    }); 

    let regex_work_url = /^(?:\/collections\/[^\/?#]+)?\/works\/(\d+)(?:\/chapters\/(\d+))?$/;
    let work_chapter = window.location.pathname.match(regex_work_url);
    let workid = 0;
    let works: number[] = [];
    let $works = $();

    export function scrollToLocation(workid: number, workchap: IWorkChapter, dojump?: boolean) {
        if (!$chapter_text) { return; }

        let had_chapter = false;

        $chapter_text.each(function (index, elem) {
            let $e = $(elem);
            let data: { num: number, id: number } = $e.data('ao3t');

            if (data.id !== workchap.chapterid) {
                return true;
            }

            had_chapter = true;

            let centre = window.innerHeight / 2;

            // First chapter on the page
            if (index === 0 && workchap.location === 0) {
                console.log("Should scroll to: %i page top", workchap.number);
                window.scrollTo(0, 0);
                return false;
            }
            // Last chapter on page
            else if (index === ($chapter_text.length - 1) && workchap.location === null) {
                console.log("Should scroll to: %i page bottom", workchap.number);
                if ($feedback_actions.length) {
                    $feedback_actions[0].scrollIntoView(false);
                }
                return false;
            }

            let paragraph: number;
            let $children = $e.children();
            if (workchap.location === null || (paragraph = Math.floor(workchap.location / LOC_PARA_MULTIPLIER)) >= $children.length) {
                // Scroll to bottom of the element
                console.log("Should scroll to: %i p end", workchap.number);
                let rect = elem.getBoundingClientRect();
                window.scrollTo(0, rect.bottom + window.scrollY - centre);
                return false;
            }

            let offset = workchap.location % LOC_PARA_MULTIPLIER;
            console.log("Should scroll to: %i p %i c %i", workchap.number, paragraph, offset);

            let $child = $children.eq(paragraph);
            let child = $child[0];
            let p_rect = child.getBoundingClientRect();

            if (offset >= LOC_PARA_BOTTOM) {
                window.scrollTo(0, p_rect.bottom + window.scrollY - centre);
                return false;
            }

            window.scrollTo(0, p_rect.top + p_rect.height * offset / LOC_PARA_FRAC_MULTIPLER + window.scrollY - centre);

            return false;
        });

        if (had_chapter) { SetCurrentLocation(Object.assign({ workid: workid }, workchap)); }

        // Change page!
        if (!had_chapter && dojump) {
            window.location.replace('/works/' + workid + (workchap.chapterid ? '/chapters/' + workchap.chapterid.toString() : '') + "#ao3tjump");
        }
    };

    let last_scroll_location : number|null = null;
    export function updateLocation() {
        if (window.pageYOffset === last_scroll_location) return null;
        last_scroll_location = window.pageYOffset;

        let workchapter: IWorkChapter | null = null;

        // Find which $userstuff is at the centre of the screen

        let centre = window.innerHeight / 2;

        // Feedback actions block is above the bottom of the screen? Declare the chapters read
        if ($feedback_actions.length && $feedback_actions[0].getBoundingClientRect().top < window.innerHeight) {
            let data: { num: number, id: number } = $chapter_text.last().data('ao3t');
            workchapter = {
                number: data.num,
                chapterid: data.id,
                location: null,
                seq: null
            };
        }
        // First chapter hasn't reached centre yet? Declare entire thing unread
        else if ($chapter_text[0].getBoundingClientRect().top > centre) {
            let data: { num: number, id: number } = $chapter_text.first().data('ao3t');
            workchapter = {
                number: data.num,
                chapterid: data.id,
                location: 0,
                seq: null,
            };
        }
        else {
            $chapter_text.each(function (index, elem) {

                let rect = elem.getBoundingClientRect();
                if (rect.top <= centre) {
                    let $e = $(elem);
                    let data: { num: number, id: number } = $e.data('ao3t');
                    let loc: number | null = 0;

                    if (rect.bottom <= centre) {
                        loc = null;
                    }
                    else {
                        // Find which paragraph of these overlaps the centre
                        $e.children().each(function (index, elem) {
                            let p_rect = elem.getBoundingClientRect();
                            if (p_rect.top <= centre) {
                                if (p_rect.bottom <= centre) {
                                    loc = index * LOC_PARA_MULTIPLIER + LOC_PARA_BOTTOM;
                                }
                                else {
                                    let frac = (centre - p_rect.top) * LOC_PARA_FRAC_MULTIPLER / p_rect.height;

                                    loc = index * LOC_PARA_MULTIPLIER + Math.floor(frac);
                                }
                            }
                            else {
                                return false;
                            }
                        });
                    }

                    workchapter = {
                        number: data.num,
                        chapterid: data.id,
                        location: loc,
                        seq: null
                    };
                }
                else {
                    return false;
                }
            });
        }

        return workchapter;
    };


    if (work_chapter && work_chapter.length === 3) {
        workid = parseInt(work_chapter[1]);
        let $chapter = $('#chapters .chapter[id]');

        // Multichapter fic
        if ($chapter.length !== 0) {
            let regex_chapter = /^chapter-(\d+)$/;

            $chapter_text = $chapter.children('.userstuff');
            // Can have one or more chapters being displayed
            $chapter_text.each(function (index, elem): void {
                let $e = $(elem);
                let $c = $e.parent();
                let $a = $c.find(".chapter > .title > a");

                let rnum = $c.attr('id').match(regex_chapter);
                let rid = $a.attr('href').match(regex_work_url);

                if (rnum !== null && typeof (rnum[1]) !== 'undefined' && rid !== null && typeof rid[2] !== 'undefined') {
                    let number = parseInt(rnum[1]);
                    let chapterid = parseInt(rid[2]);

                    if (number !== NaN && chapterid !== NaN) {
                        $e.data('ao3t', {
                            num: number,
                            id: chapterid,
                        });
                    }
                }

            });
        }
        // Single chapter fic
        else {
            $chapter = $('#chapters');
            $chapter_text = $chapter.children('.userstuff');
            $chapter_text.data('ao3t', { num: 1, id: 0 });
        }

        let last_location = updateLocation();
        if (last_location) { SetCurrentLocation(Object.assign({ workid: workid }, last_location)); }
        let last_set_location = last_location;
        if (last_set_location) {
            SetWorkChapters({ [workid]: last_set_location });
        }

        setInterval(() => {
            let new_location = updateLocation();
            if (new_location) { SetCurrentLocation(Object.assign({ workid: workid }, new_location)); }
            if (new_location && (!last_location || new_location.number > last_location.number ||
                (new_location.number === last_location.number && last_location.location !== null &&
                    (new_location.location === null || new_location.location > last_location.location)))) {
                last_location = new_location;

                if (last_location.location === null) {
                    EnableLastLocationJump(workid, last_location);
                    SetWorkChapters({ [workid]: last_set_location = last_location });
                }
            }
        }, 500);

        setInterval(() => {
            if (last_location && (!last_set_location || last_location.number > last_set_location.number ||
                (last_location.number === last_set_location.number && last_set_location.location !== null &&
                    (last_location.location === null || last_location.location > last_set_location.location)))) {
                EnableLastLocationJump(workid, last_location);
                SetWorkChapters({ [workid]: last_set_location = last_location });
            }
        }, 5000);

        works.push(workid);
        $works = $('.chapters-show .work .work.meta, .works-show .work.meta').first();
    } else {
        // Might be a listing page with blurbs
        let regex_work = /^work_(\d+)$/;
        $works = $('.work.blurb[id]');
        $works.each(function (index, elem) {
            let $work = $(elem);
            let attr = $work.attr('id').match(regex_work);
            if (attr !== null) {
                works.push(parseInt(attr[1]));
            }
        });
    }

    let regex_chapter_count = /^(\d+)\/(\d+|\?)/;
    const in_reading_list_html = '<abbr class="ao3-track-inlist" title="Ao3Track: In Reading List">' + String.fromCodePoint(0x1F4DA) + "</abbr>";
    const unfinished_html = '<abbr class="ao3-track-inlist" title="Ao3Track: Unread chapters">' + String.fromCodePoint(0x1F4D1) + "</abbr>";
    const read_all_html  = '<abbr class="ao3-track-readall" title="Ao3Track: Read all chapters">' + String.fromCodePoint(0x1F4DC) + "</abbr>";

    export function ExtendWorkSummary($work: JQuery, workid: number, workchap: IWorkChapter, inreadinglist : boolean) {
        $work.find(".stats .lastchapters").remove();

        let $chapters = $work.find(".stats dd.chapters");
        let chapters_text = $chapters.text().match(regex_chapter_count);

        let str_id = workchap.chapterid.toString();
        let chapters_finished = workchap.number;
        if (workchap.location !== null) { chapters_finished--; }
        let chapter_path = '/works/' + workid + (workchap.chapterid ? '/chapters/' + str_id : '');
        $chapters.prepend('<a href="' + chapter_path + '#ao3tjump">' + chapters_finished.toString() + '</a>/');

        if (chapters_text === null) { return; }

        let $blurb_heading = $work.find('.header h4.heading');
        if ($blurb_heading.length) {

            let chapter_count = parseInt(chapters_text[1]);
            let chapter_total = chapters_text[2] !== "?" ? parseInt(chapters_text[1]):null;

            if (chapter_count > chapters_finished) {
                let unread = chapter_count - chapters_finished;
                $blurb_heading.append(' ', '<span class="ao3-track-new">(<a href="' + chapter_path + '#ao3tjump">' + unread + "\xA0unread chapter" + (unread === 1 ? '' : 's') + '</a>)</span>');
                $blurb_heading.prepend(unfinished_html);                
            }
            else if (chapter_total === chapters_finished) {
                $blurb_heading.prepend(read_all_html);                
            }

            if (inreadinglist) {                
                $blurb_heading.prepend(in_reading_list_html);
            }
        }
    }

   if (typeof jumpnow === "object" ) { scrollToLocation(workid, jumpnow, false); }

    let workchaps: { [key: number]: IWorkChapter } | null = null;
    let readinglist: { [key: string]: boolean } | null = null;

    function iterateWorks(workchaps: { [key: number]: IWorkChapter | undefined}, readinglist: { [key: string]: boolean| undefined}) {
        for (let i = 0; i < $works.length && i < works.length; i++) {
            let inreadinglist = readinglist["http://archiveofourown.org/works/"+works[i]] || false;
            if (inreadinglist || works[i] in workchaps) {
                let workchap = workchaps[works[i]] || { number: 1, chapterid: 0, location: 0, seq: 0};
                if (works[i] === workid) {
                    EnableLastLocationJump(workid, workchap);
                    if (jumpnow === true) { scrollToLocation(workid, workchap, false); }
                }

                ExtendWorkSummary($($works[i]), works[i], workchap, inreadinglist);
            }
        }
    }

    Ao3Track.AreUrlsInReadingList(works.map((id) => "http://archiveofourown.org/works/"+id), (response) => {
        readinglist = response;
        if (workchaps !== null) iterateWorks(workchaps, readinglist);
    });

    Ao3Track.GetWorkChapters(works, (response) => {
        workchaps = response;
        if (readinglist !== null) iterateWorks(workchaps, readinglist);
    });

    let $serieses = $('.series a[href^="/series/"]');
    Ao3Track.AreUrlsInReadingList($serieses.toArray().map((e) => {
        let a = e as HTMLAnchorElement;
        if (a === null) return "";
        return a.href;
    }), (response)=> {
        $serieses.each((i,e)=>{
            let a = e as HTMLAnchorElement;
            if (a === null) return;
            
            if (response[a.href]) {
                $(a).prepend(in_reading_list_html);
            }
        });
    });

    // Add sort direction to the work-filters form
    $("form#work-filters").each((index,form) => {
        let $form = $(form);

        if ($form.find('input[name="work_search[sort_direction]"], select[name="work_search[sort_direction]"]').length !== 0)
            return;

        let $sort_column = $form.find('select[name="work_search[sort_column]"]');
        if ($sort_column.length === 0) 
            return;

        let $sort_colomn_dd = $sort_column.parent("dd");
        if ($sort_colomn_dd.length === 0) 
            return;

        let sort_direction = search_params["work_search[sort_direction]"] !== undefined ? search_params["work_search[sort_direction]"][0] || "" : "";

        let unset_option = document.createElement("option");
        unset_option.text="";
        unset_option.value="";

        let asc_option = document.createElement("option");
        asc_option.text="Ascending";
        asc_option.value="asc";

        let desc_option = document.createElement("option");
        desc_option.text="Descending";
        desc_option.value="desc";
        
        let select = document.createElement("select");
        select.id = "work_search_sort_direction";
        select.name = "work_search[sort_direction]";
        select.options.add(unset_option);
        select.options.add(asc_option);
        select.options.add(desc_option);

        unset_option.selected = sort_direction === unset_option.value;
        asc_option.selected = sort_direction === asc_option.value;
        desc_option.selected = sort_direction === desc_option.value;

        let dd = document.createElement("dd");
        dd.appendChild(select);

        let label = document.createElement("label");
        label.innerText = "Sort Direction";
        label.htmlFor = "work_search_sort_direction";

        let dt = document.createElement("dt");
        dt.appendChild(label);

        $sort_colomn_dd.after([dt, dd]);
    });
}
