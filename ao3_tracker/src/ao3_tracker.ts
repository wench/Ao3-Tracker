namespace Ao3Track {
    const $ = jQuery;

    // Enable swipe left to go to next page
    let $next = $('head link[rel=next], .chapter.next > a, .pagination > .next > a, #series a:contains(\xBB)');
    if ($next.length > 0) { SetNextPage($next.attr('href')); }

    let $prev = $('.head link[rel=prev], chapter.previous > a, .pagination > .previous > a, #series a:contains(\xAB)');
    if ($prev.length > 0) { SetPrevPage($next.attr('href')); }

    const LOC_PARA_MULTIPLIER = 1000000000;
    const LOC_PARA_BOTTOM = 500000000;
    const LOC_PARA_FRAC_MULTIPLER = 479001600;

    let $feedback_actions = $('#feedback > .actions');
    let $chapter_text = $();    

    let jumpnow = window.location.hash === "#ao3t:jump";
    if (jumpnow) {
        history.replaceState({}, document.title, window.location.href.substr(0,window.location.href.indexOf('#')));
    }

    let regex_work_url = /^(?:\/collections\/[^\/?#]+)?\/works\/(\d+)(?:\/chapters\/(\d+))?$/;
    let work_chapter = window.location.pathname.match(regex_work_url);
    let workid = 0;
    let works: number[] = [];
    let $works = $();

    export function scrollToLocation (workid: number, workchap: IWorkChapter, dojump? : boolean) {
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

        if (had_chapter) { SetCurrentLocation(Object.assign({workid:workid},workchap)); }

        // Change page!
        if (!had_chapter && dojump) {
            window.location.replace('/works/' + workid + (workchap.chapterid ? '/chapters/' + workchap.chapterid.toString() : '') + "#ao3t:jump");
        }
    };

    export function updateLocation () {
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

                let rnum = $chapter.attr('id').match(regex_chapter);
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
        if (last_location !== null) { SetCurrentLocation(Object.assign({workid:workid},last_location)); }
        let last_set_location = last_location;
        if (last_set_location) {
            SetWorkChapters({ [workid]: last_set_location });
        }

        $(window).scroll((eventObject) => {
            let new_location = updateLocation();
            if (new_location !== null) { SetCurrentLocation(Object.assign({workid:workid},new_location)); }
            if (new_location && (!last_location || new_location.number > last_location.number ||
                 (new_location.number === last_location.number && last_location.location !== null && 
                  (new_location.location === null || new_location.location > last_location.location))))
            { 
                last_location = new_location;

                if (last_location.location === null) {
                    EnableLastLocationJump(workid,last_location);
                    SetWorkChapters({ [workid]: last_set_location = last_location });
                }
            }
        });

        setInterval(() => {
            if (last_location && (!last_set_location || last_location.number > last_set_location.number ||
                 (last_location.number === last_set_location.number && last_set_location.location !== null &&
                  (last_location.location === null || last_location.location > last_set_location.location)))) 
            {
                EnableLastLocationJump(workid,last_location);
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

    let regex_chapter_count = /^(\d+)\//;

    export function ExtendWorkSummary($work: JQuery, workid: number, workchap : IWorkChapter)
    {
        $work.find(".stats .lastchapters").remove();

        let $chapters = $work.find(".stats dd.chapters");
        let chapters_text = $chapters.text().match(regex_chapter_count);

        let str_id = workchap.chapterid.toString();
        let chapters_finished = workchap.number;
        if (workchap.location !== null) { chapters_finished--; }
        let chapter_path = '/works/' + workid + (workchap.chapterid ? '/chapters/' + str_id : '');
        $chapters.prepend('<a href="' + chapter_path + '#ao3t:jump">' + chapters_finished.toString() + '</a>/');

        if (chapters_text === null) { return; }

        let $blurb_heading = $work.find('.header h4.heading');
        if ($blurb_heading.length) {

            let chapter_count = parseInt(chapters_text[1]);

            if (chapter_count > chapters_finished) {
                let unread = chapter_count - chapters_finished;
                $blurb_heading.append(' ', '<span class="ao3-track-new">(<a href="' + chapter_path + '#ao3t:jump" target="_blank">' + unread + ' unread chapter' + (unread === 1 ? '' : 's') + '</a>)</span>');
            }
        }
    }

    GetWorkChapters(works, (it) => {
        for (let i = 0; i < $works.length && i < works.length; i++) {
            if (works[i] in it) {
                let workchap = it[works[i]];
                if (works[i] === workid) {
                    EnableLastLocationJump(workid, workchap);
                    if (jumpnow) { scrollToLocation(workid, workchap, false); }
                }

                ExtendWorkSummary($($works[i]), works[i], workchap);
            }
        }
    });
}