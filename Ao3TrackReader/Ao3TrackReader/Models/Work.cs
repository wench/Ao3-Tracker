using System;
using SQLite;
using Ao3TrackReader.Helper;

namespace Ao3TrackReader
{
    public class Work : IWorkChapterEx
    {
        public Work()
        {
        }

        [PrimaryKey, Column("id")]
        public long workid { get; set; }
        public long chapterid { get; set; }
        public long number { get; set; }
        public long timestamp { get; set; }
        public long? location { get; set; }
        public long seq { get; set; }

        long? IWorkChapter.seq { get { return seq; } set { seq = value ?? 0; } }
        
        public bool IsNewer(Work newitem)
        {
            if (newitem.workid != workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.seq > this.seq) { return true; }
            else if (newitem.seq < this.seq) { return false; }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (this.location == null) { return false; }
            if (newitem.location == null) { return true; }

            return newitem.location > this.location;
        }
        public bool IsNewer(IWorkChapter newitem)
        {
            if (newitem.seq > this.seq) { return true; }
            else if (newitem.seq < this.seq) { return false; }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (this.location == null) { return false; }
            if (newitem.location == null) { return true; }

            return newitem.location > this.location;
        }
        public bool IsNewer(IWorkChapterEx newitem)
        {
            if (newitem.workid != workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.seq > this.seq) { return true; }
            else if (newitem.seq < this.seq) { return false; }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (this.location == null) { return false; }
            if (newitem.location == null) { return true; }

            return newitem.location > this.location;
        }
        public bool IsNewerOrSame(Work newitem)
        {
            if (newitem.workid != workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.seq > this.seq) { return true; }
            else if (newitem.seq < this.seq) { return false; }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (newitem.location == null) { return true; }
            if (this.location == null) { return false; }

            return newitem.location >= this.location;
        }

        public bool IsNewerOrSame(IWorkChapter newitem)
        {
            if (newitem.seq > this.seq) { return true; }
            else if (newitem.seq < this.seq) { return false; }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (newitem.location == null) { return true; }
            if (this.location == null) { return false; }

            return newitem.location >= this.location;
        }
        public bool IsNewerOrSame(IWorkChapterEx newitem)
        {
            if (newitem.workid != workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.seq > this.seq) { return true; }
            else if (newitem.seq < this.seq) { return false; }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (newitem.location == null) { return true; }
            if (this.location == null) { return false; }

            return newitem.location >= this.location;
        }

    }
}

