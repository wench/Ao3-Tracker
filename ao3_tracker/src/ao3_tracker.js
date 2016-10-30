(function () {
    // Add swipe left support for Microsoft Edge to to to next page
    var $next = $('.chapter.next > a').first();
    if ($next.length > 0) {
        $('<link rel="next"></link>').attr('href', $next.attr('href')).appendTo('head');
    }
    var LOC_PARA_MULTIPLIER = 1000000000;
    var LOC_PARA_BOTTOM = 500000000;
    var LOC_PARA_FRAC_MULTIPLER = 479001600;
    var works = [];
    var $works;
    var regex_work_url = /^\/works\/(\d+)(?:\/chapters\/(\d+))?$/;
    var work_chapter = window.location.pathname.match(regex_work_url);
    var scroll_to_location = null;
    var workid = 0;
    if (work_chapter && work_chapter.length === 3) {
        workid = parseInt(work_chapter[1]);
        var $chapter_1 = $('#chapters .chapter[id]');
        var $chapter_text_1;
        // Multichapter fic
        if ($chapter_1.length !== 0) {
            var regex_chapter_1 = /^chapter-(\d+)$/;
            $chapter_text_1 = $chapter_1.children('.userstuff');
            // Can have one or more chapters being displayed
            $chapter_text_1.each(function (index, elem) {
                var $e = $(elem);
                var $c = $e.parent();
                var $a = $c.find(".chapter > .title > a");
                var rnum = $chapter_1.attr('id').match(regex_chapter_1);
                var rid = $a.attr('href').match(regex_work_url);
                if (rnum !== null && typeof (rnum[1]) !== 'undefined' && rid !== null && typeof rid[2] !== 'undefined') {
                    var number = parseInt(rnum[1]);
                    var chapterid = parseInt(rid[2]);
                    if (number !== NaN && chapterid !== NaN) {
                        $e.data('ao3t', {
                            num: number,
                            id: chapterid,
                        });
                    }
                }
            });
        }
        else {
            $chapter_1 = $('#chapters');
            $chapter_text_1 = $chapter_1.children('.userstuff');
            $chapter_text_1.data('ao3t', { num: 1, id: 0 });
        }
        var $feedback_actions_1 = $('#feedback > .actions');
        var updateLocation = function () {
            var workchapter = null;
            // Find which $userstuff is at the centre of the screen
            var centre = window.innerHeight / 2;
            // Feedback actions block is above the bottom of the screen? Declare the chapters read
            var fsbcr = $feedback_actions_1[0].getBoundingClientRect();
            if (fsbcr.top < window.innerHeight) {
                var data = $chapter_text_1.last().data('ao3t');
                workchapter = {
                    number: data.num,
                    chapterid: data.id,
                    location: null
                };
            }
            else if ($chapter_text_1[0].getBoundingClientRect().top > centre) {
                var data = $chapter_text_1.first().data('ao3t');
                workchapter = {
                    number: data.num,
                    chapterid: data.id,
                    location: 0
                };
            }
            else {
                $chapter_text_1.each(function (index, elem) {
                    var rect = elem.getBoundingClientRect();
                    if (rect.top <= centre) {
                        var $e = $(elem);
                        var data = $e.data('ao3t');
                        var loc_1 = 0;
                        if (rect.bottom <= centre) {
                            loc_1 = null;
                        }
                        else {
                            // Find which paragraph of these overlaps the centre
                            $e.children().each(function (index, elem) {
                                var p_rect = elem.getBoundingClientRect();
                                if (p_rect.top <= centre) {
                                    if (p_rect.bottom <= centre) {
                                        loc_1 = index * LOC_PARA_MULTIPLIER + LOC_PARA_BOTTOM;
                                    }
                                    else {
                                        var frac = (centre - p_rect.top) * LOC_PARA_FRAC_MULTIPLER / p_rect.height;
                                        loc_1 = index * LOC_PARA_MULTIPLIER + Math.floor(frac);
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
                            location: loc_1
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
            var setmsg = { type: "SET", data: {} };
            setmsg.data[workid] = workchapter;
            chrome.runtime.sendMessage(setmsg);
        };
        scroll_to_location = function (workchap) {
            $chapter_text_1.each(function (index, elem) {
                var $e = $(elem);
                var data = $e.data('ao3t');
                if (data.id !== workchap.chapterid) {
                    return true;
                }
                var centre = window.innerHeight / 2;
                // First chapter on the page
                if (index === 0 && workchap.location === 0) {
                    console.log("Should scroll to: %i page top", workchap.number);
                    window.scrollTo(0, 0);
                    return false;
                }
                else if (index === ($chapter_text_1.length - 1) && workchap.location === null) {
                    console.log("Should scroll to: %i page bottom", workchap.number);
                    $feedback_actions_1[0].scrollIntoView(false);
                    return false;
                }
                var paragraph;
                var $children = $e.children();
                if (workchap.location === null || (paragraph = Math.floor(workchap.location / LOC_PARA_MULTIPLIER)) >= $children.length) {
                    // Scroll to bottom of the element
                    console.log("Should scroll to: %i p end", workchap.number);
                    var rect = elem.getBoundingClientRect();
                    window.scrollTo(0, rect.bottom + window.scrollY - centre);
                    return false;
                }
                var offset = workchap.location % LOC_PARA_MULTIPLIER;
                console.log("Should scroll to: %i p %i c %i", workchap.number, paragraph, offset);
                var $child = $children.eq(paragraph);
                var child = $child[0];
                var p_rect = child.getBoundingClientRect();
                if (offset >= LOC_PARA_BOTTOM) {
                    window.scrollTo(0, p_rect.bottom + window.scrollY - centre);
                    return false;
                }
                window.scrollTo(0, p_rect.top + p_rect.height * offset / LOC_PARA_FRAC_MULTIPLER + window.scrollY - centre);
                return false;
            });
        };
        updateLocation();
        // $(window).scroll(updateLocation); 
        setInterval(5000, updateLocation);
        works.push(workid);
        $works = $('.chapters-show .work .work.meta, .works-show .work.meta').first();
    }
    else {
        // Might be a listing page with blurbs
        var regex_work_1 = /^work_(\d+)$/;
        $works = $('.work.blurb[id]');
        $works.each(function (index, elem) {
            var $work = $(elem);
            var attr = $work.attr('id').match(regex_work_1);
            if (attr !== null) {
                works.push(parseInt(attr[1]));
            }
        });
    }
    var $actions = $('<div class=" actions" id="ao3t-actions"a></div>').appendTo("#outer");
    var $actions_ul = $('<ul></ul>').appendTo($actions);
    var $sync_now = $('<li><a href="#" name="ao3t-sync-now">Sync Now</a></li>').appendTo($actions_ul);
    var last_location;
    var $goto_last_location = $('<li><a href="#" name="ao3t-last-loc">Jump to previous</a></li>');
    $sync_now.click(function (eventObject) {
        eventObject.preventDefault();
        eventObject.stopImmediatePropagation();
        var syncmsg = { type: "DO_SYNC" };
        chrome.runtime.sendMessage(syncmsg, function (result) {
        });
    });
    $goto_last_location.click(function (eventObject) {
        eventObject.preventDefault();
        eventObject.stopImmediatePropagation();
        if (scroll_to_location && last_location) {
            scroll_to_location(last_location);
        }
    });
    var getmsg = { type: "GET", data: works };
    chrome.runtime.sendMessage(getmsg, function (it) {
        var regex_chapter_count = /^(\d+)\//;
        for (var i = 0; i < $works.length && i < works.length; i++) {
            if (works[i] in it) {
                var workchap = it[works[i]];
                if (scroll_to_location && works[i] === workid) {
                    last_location = workchap;
                    $goto_last_location.appendTo($actions_ul);
                }
                var $work = $($works[i]);
                $work.find(".stats .lastchapters").remove();
                var $chapters = $work.find(".stats dd.chapters");
                var str_id = workchap.chapterid.toString();
                var str_num = workchap.number.toString();
                var chapter_path = '/works/' + works[i] + (workchap.chapterid ? '/chapters/' + str_id : '');
                $chapters.after('<dt class="ao3-track-last">Last:</dt>', '<dd class="ao3-track-last"><a href="' + chapter_path + '">' + str_num + '</a></dd>');
                var $blurb_heading = $work.find('.header h4.heading');
                if ($blurb_heading.length) {
                    var chapters_text = $chapters.text().match(regex_chapter_count);
                    if (chapters_text === null) {
                        continue;
                    }
                    var chapter_count = parseInt(chapters_text[1]);
                    var chapters_finished = workchap.number;
                    if (workchap.location !== null) {
                        chapters_finished--;
                    }
                    if (chapter_count > chapters_finished) {
                        var unread = chapter_count - chapters_finished;
                        $blurb_heading.append(' ', '<span class="ao3-track-new">(<a href="' + chapter_path + '" target="_blank">' + unread + ' unfinished chapter' + (unread === 1 ? '' : 's') + '</a>)</span>');
                    }
                }
            }
        }
    });
})();
//# sourceMappingURL=ao3_tracker.js.map