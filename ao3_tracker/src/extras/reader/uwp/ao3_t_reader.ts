/// <reference path="jsinterop.d.ts" />

namespace Ao3Track {

    // This is a mess. Need to manually marshal between { [key: number]: IWorkChapter } and IDictionary<long,WorkChapter>

    function ToAssocArray<V>(map : Ao3TrackHelper.IIterable<Ao3TrackHelper.IKeyValuePair<number,V>>) : { [key: number]: V } {
            var response : { [key: number]: V } = {};
            for (var it = map.first(); it.hasCurrent; it.moveNext()) {
                var i = it.current;
                response[i.key] = i.value;
            }
            return response;
    }

    export function GetWorkChapters(works: number[], callback: (workchapters: GetWorkChaptersMessageResponse) => void) {
        Ao3TrackHelper.getWorkChaptersAsync(works).then((result) => {
            callback(ToAssocArray<IWorkChapter>(result));
        });
    }

    export function SetWorkChapters(workchapters: { [key: number]: IWorkChapter; }) {
        var m = Ao3TrackHelper.createWorkChapterMap();
        for (let key in workchapters) {
            m.insert(key as any, Ao3TrackHelper.createWorkChapter(workchapters[key].number, workchapters[key].chapterid, workchapters[key].location));
        }
        Ao3TrackHelper.setWorkChapters(m);
    }   

    export function DisableLastLocationJump()    {
        Ao3TrackHelper.enableJumpToLastLocation(false);
        Ao3TrackHelper.onjumptolastlocationevent = null;
    }

    export function EnableLastLocationJump(lastloc: IWorkChapter)    {
        Ao3TrackHelper.onjumptolastlocationevent = (ev) => { Ao3Track.scrollToLocation(lastloc); }
        Ao3TrackHelper.enableJumpToLastLocation(true);
    }

};
