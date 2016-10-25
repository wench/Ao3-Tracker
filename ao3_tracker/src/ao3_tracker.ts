// Add swipe left support for Microsoft Edge to to to next page
var $next = $('.chapter.next > a').first();
if ($next.length > 0) { $('<link rel="next"></link>').attr('href', $next.attr('href')).appendTo('head'); }

const LOC_PARA_MULTIPLIER = 1000000000;
const LOC_PARA_BOTTOM = 500000000;
const LOC_PARA_FRAC_MULTIPLER = 479001600;

let works: number[] = [];
let $works: JQuery;

let regex_work_url = /^\/works\/(\d+)(?:\/chapters\/(\d+))?$/;
let work_chapter = window.location.pathname.match(regex_work_url);
let scroll_to_location: ((workchap: IWorkChapter) => void) | null = null;
let workid = 0;
if (work_chapter && work_chapter.length === 3) {
    workid = parseInt(work_chapter[1]);
    let $chapter = $('#chapters .chapter[id]');
    let $chapter_text: JQuery;

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

    let $feedback_actions = $('#feedback > .actions');

    let updateLocation = () => {
        let workchapter: IWorkChapter | null = null;

        // Find which $userstuff is at the centre of the screen

        let centre = window.innerHeight / 2;

        // Feedback actions block is above the bottom of the screen? Declare the chapters read
        var fsbcr = $feedback_actions[0].getBoundingClientRect();
        if (fsbcr.top < window.innerHeight) {
            let data: { num: number, id: number } = $chapter_text.last().data('ao3t');
            workchapter = {
                number: data.num,
                chapterid: data.id,
                location: null
            };
        }
        // First chapter hasn't reached centre yet? Declare entire thing unread
        else if ($chapter_text[0].getBoundingClientRect().top > centre) {
            let data: { num: number, id: number } = $chapter_text.first().data('ao3t');
            workchapter = {
                number: data.num,
                chapterid: data.id,
                location: 0
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
                        location: loc
                    };
                }
                else {
                    return false;
                }
            });
        }

        if (workchapter === null) {
            return;
        }

        let setmsg: SetWorkChaptersMessage = { type: "SET", data: {} };
        setmsg.data[workid] = workchapter;
        chrome.runtime.sendMessage(setmsg);
    };

    scroll_to_location = (workchap: IWorkChapter) => {

        $chapter_text.each(function (index, elem) {
            let $e = $(elem);
            let data: { num: number, id: number } = $e.data('ao3t');

            if (data.id !== workchap.chapterid) {
                return true;
            }

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
                $feedback_actions[0].scrollIntoView(false);
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

            window.scrollTo(0, p_rect.top + p_rect.height*offset/LOC_PARA_FRAC_MULTIPLER + window.scrollY - centre);

            return false;
        });
    };

    updateLocation();
    $(window).scroll(updateLocation);

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

let $actions = $('<div class=" actions" id="ao3t-actions"a></div>').appendTo("#outer");
let $actions_ul = $('<ul></ul>').appendTo($actions);
let $sync_now = $('<li><a href="#" name="ao3t-sync-now">Sync Now</a></li>').appendTo($actions_ul);
let $goto_last_location = $('<li><a href="#" name="ao3t-last-loc">Jump to previous</a></li>');

$sync_now.click((eventObject) => {
    eventObject.preventDefault();
    eventObject.stopImmediatePropagation();

    let syncmsg : DoSyncMessage = { type: "DO_SYNC" };
    chrome.runtime.sendMessage(syncmsg, function (result : boolean) {
    });        
});

$goto_last_location.click((eventObject) => {
    eventObject.preventDefault();
    eventObject.stopImmediatePropagation();
    
    if (scroll_to_location) {
        scroll_to_location($goto_last_location.data("ao3t-workchap"));
    }    
});

let getmsg: GetWorkChaptersMessage = { type: "GET", data: works };
chrome.runtime.sendMessage(getmsg, function (it: GetWorkChaptersMessageResponse) {
    let regex_chapter_count = /^(\d+)\//;
    for (let i = 0; i < $works.length && i < works.length; i++) {
        if (works[i] in it) {
            let workchap = it[works[i]];
            if (scroll_to_location && works[i] === workid) {                
                 $goto_last_location.data("ao3t-workchap",workchap).appendTo($actions_ul); 
            }
            let $work = $($works[i]);
            $work.find(".stats .lastchapters").remove();

            let $chapters = $work.find(".stats dd.chapters");
            let str_id = workchap.chapterid.toString();
            let str_num = workchap.number.toString();
            let chapter_path = '/works/' + works[i] + (workchap.chapterid ? '/chapters/' + str_id : '');
            $chapters.after('<dt class="ao3-track-last">Last:</dt>', '<dd class="ao3-track-last"><a href="' + chapter_path + '">' + str_num + '</a></dd>');

            let $blurb_heading = $work.find('.header h4.heading');
            if ($blurb_heading.length) {
                let chapters_text = $chapters.text().match(regex_chapter_count);
                if (chapters_text === null) { continue; }

                let chapter_count = parseInt(chapters_text[1]);
                let chapters_finished = workchap.number;
                if (workchap.location !== null) { chapters_finished--; }

                if (chapter_count > chapters_finished) {
                    let unread = chapter_count - chapters_finished;
                    $blurb_heading.append(' ', '<span class="ao3-track-new">(<a href="' + chapter_path + '" target="_blank">' + unread + ' unfinished chapter' + (unread === 1 ? '' : 's') + '</a>)</span>');
                }

            }
        }
    }
});

