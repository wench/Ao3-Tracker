using System;
using SQLite;
using Ao3TrackReader.Helper;

namespace Ao3TrackReader
{
    public class Work : IWorkChapter
    {
        public Work()
        {
        }


        [PrimaryKey]
        public long id { get; set; }
        public long chapterid { get; set; }
        public long number { get; set; }
        public long timestamp { get; set; }
        public long? location { get; set; }


        public bool IsNewer(Work newitem)
        {
            if (newitem.id != id) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (this.location == null) { return false; }
            if (newitem.location == null) { return true; }

            return newitem.location > this.location;
        }
        public bool IsNewer(IWorkChapter newitem)
        {
            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (this.location == null) { return false; }
            if (newitem.location == null) { return true; }

            return newitem.location > this.location;
        }

        public bool IsNewerOrSame(Work newitem)
        {
            if (newitem.id != id) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (newitem.location == null) { return true; }
            if (this.location == null) { return false; }

            return newitem.location >= this.location;
        }

        public bool IsNewerOrSame(IWorkChapter newitem)
        {
            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (newitem.location == null) { return true; }
            if (this.location == null) { return false; }

            return newitem.location >= this.location;
        }

    }
}

