var $next = $('.chapter.next > a').first();
if ($next.length) { $('<link rel="next"></link>').attr('href', $next.attr('href')).appendTo('head'); }


let works: number[] = [];
let $works: JQuery;

let regex_work_url = /^\/works\/(\d+)(?:\/chapters\/(\d+))?$/;
let work_chapter = window.location.pathname.match(regex_work_url);
if (work_chapter && work_chapter.length === 3) {
    let workid = parseInt(work_chapter[1]);
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

    let updateLocation = function () {
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
                                    loc = index * 1000000000 + 999999999;
                                }
                                else {
                                    loc = index * 1000000000;
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

    updateLocation();
    setInterval(updateLocation,1000);

    works.push(workid);
    $works = $('.chapters-show .work .work.meta, .works-show .work.meta');
} else {
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

let getmsg: GetWorkChaptersMessage = { type: "GET", data: works };
chrome.runtime.sendMessage(getmsg, function (it: GetWorkChaptersMessageResponse) {
    let regex_chapter_count = /^(\d+)\//;
    for (let i = 0; i < $works.length && i < works.length; i++) {
        if (works[i] in it && "number" in it[works[i]] && "chapterid" in it[works[i]]) {
            let $work = $($works[i]);
            $work.find(".stats .lastchapters").remove();

            let $chapters = $work.find(".stats dd.chapters");
            let str_id = it[works[i]].chapterid.toString();
            let str_num = it[works[i]].number.toString();
            let chapter_path = '/works/' + works[i] + (it[works[i]].chapterid ? '/chapters/' + str_id : '');
            $chapters.after('<dt class="ao3-track-last">Last:</dt>', '<dd class="ao3-track-last"><a href="' + chapter_path + '">' + str_num + '</a></dd>');

            let $blurb_heading = $work.find('.header h4.heading');
            if ($blurb_heading.length) {
                let chapters_text = $chapters.text().match(regex_chapter_count);
                if (chapters_text === null) { continue; }

                let chapter_count = parseInt(chapters_text[1]);
                let chapters_finished = it[works[i]].number;
                if (it[works[i]].location !== null) { chapters_finished--; }

                if (chapter_count > chapters_finished) {
                    let unread = chapter_count - chapters_finished;
                    $blurb_heading.append(' ', '<span class="ao3-track-new">(<a href="' + chapter_path + '" target="_blank">' + unread + ' unfinished chapter' + (unread === 1 ? '' : 's') + '</a>)</span>');
                }

            }
        }
    }
});

