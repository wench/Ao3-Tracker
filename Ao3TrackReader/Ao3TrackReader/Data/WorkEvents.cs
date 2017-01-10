using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Data
{
    public class WorkEvents
    {
        static Dictionary<long, WorkEvents> events = new Dictionary<long, WorkEvents>();

        public static WorkEvents TryGetEvent(long workid)
        {
            WorkEvents e;
            if (events.TryGetValue(workid, out e))
                return e;
            return null;
        }

        public static WorkEvents GetEvent(long workid)
        {
            WorkEvents e;
            if (events.TryGetValue(workid, out e))
                return e;
            return events[workid] = new WorkEvents();
        }

        public event EventHandler<Work> ChapterNumChanged;

        public void OnChapterNumChanged(object sender,Work w)
        {
            ChapterNumChanged?.Invoke(sender, w);
        }

        public event EventHandler<Work> LocationChanged;

        public void OnLocationChanged(object sender, Work w)
        {
            LocationChanged?.Invoke(sender, w);
        }

    }


}
