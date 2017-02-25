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
        public long Workid { get; set; }
        [Column("chapterid")]
        public long Chapterid { get; set; }
        [Column("number")]
        public long Number { get; set; }
        [Column("timestamp")]
        public long Timestamp { get; set; }
        [Column("location")]
        public long? Location { get; set; }
        [Column("seq")]
        public long Seq { get; set; }

        [Ignore]
        long? IWorkChapter.Seq { get { return Seq; } set { Seq = value ?? 0; } }

        [Ignore]
        public long Paragraph
        {
            get
            {
                if (Location == null) return long.MaxValue;
                return (long)Location / 1000000000L;
            }
        }

        [Ignore]
        public long Frac
        {
            get
            {
                if (Location == null) return long.MaxValue;
                var offset = (long)Location % 1000000000L;
                if (offset == 500000000L) return 100;
                return offset * 100L / 479001600L;
            }
        }

        public bool LessThan(Work newitem)
        {
            if (newitem.Workid != Workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.Seq > this.Seq) { return true; }
            else if (newitem.Seq < this.Seq) { return false; }

            if (newitem.Number > this.Number) { return true; }
            else if (newitem.Number < this.Number) { return false; }

            if (this.Location == null) { return false; }
            if (newitem.Location == null) { return true; }

            return newitem.Location > this.Location;
        }
        public bool LessThan(IWorkChapter newitem)
        {
            if (newitem is IWorkChapterEx) return LessThan(newitem as IWorkChapterEx);

            if (newitem.Seq > this.Seq) { return true; }
            else if (newitem.Seq < this.Seq) { return false; }

            if (newitem.Number > this.Number) { return true; }
            else if (newitem.Number < this.Number) { return false; }

            if (this.Location == null) { return false; }
            if (newitem.Location == null) { return true; }

            return newitem.Location > this.Location;
        }
        public bool LessThan(IWorkChapterEx newitem)
        {
            if (newitem.Workid != Workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.Seq > this.Seq) { return true; }
            else if (newitem.Seq < this.Seq) { return false; }

            if (newitem.Number > this.Number) { return true; }
            else if (newitem.Number < this.Number) { return false; }

            if (this.Location == null) { return false; }
            if (newitem.Location == null) { return true; }

            return newitem.Location > this.Location;
        }
        public bool LessThanOrEqual(Work newitem)
        {
            if (newitem.Workid != Workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.Seq > this.Seq) { return true; }
            else if (newitem.Seq < this.Seq) { return false; }

            if (newitem.Number > this.Number) { return true; }
            else if (newitem.Number < this.Number) { return false; }

            if (newitem.Location == null) { return true; }
            if (this.Location == null) { return false; }

            return newitem.Location >= this.Location;
        }

        public bool LessThanOrEqual(IWorkChapter newitem)
        {
            if (newitem is IWorkChapterEx) return LessThanOrEqual(newitem as IWorkChapterEx);

            if (newitem.Seq > this.Seq) { return true; }
            else if (newitem.Seq < this.Seq) { return false; }

            if (newitem.Number > this.Number) { return true; }
            else if (newitem.Number < this.Number) { return false; }

            if (newitem.Location == null) { return true; }
            if (this.Location == null) { return false; }

            return newitem.Location >= this.Location;
        }
        public bool LessThanOrEqual(IWorkChapterEx newitem)
        {
            if (newitem.Workid != Workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.Seq > this.Seq) { return true; }
            else if (newitem.Seq < this.Seq) { return false; }

            if (newitem.Number > this.Number) { return true; }
            else if (newitem.Number < this.Number) { return false; }

            if (newitem.Location == null) { return true; }
            if (this.Location == null) { return false; }

            return newitem.Location >= this.Location;
        }

        public static bool operator >=(Work left, Work right)
        {
            return !left.LessThan(right);
        }
        public static bool operator <=(Work left, Work right)
        {
            return left.LessThanOrEqual(right);
        }
        public static bool operator >=(Work left, WorkChapter right)
        {
            return !left.LessThan(right);
        }
        public static bool operator <=(Work left, WorkChapter right)
        {
            return left.LessThanOrEqual(right);
        }
        public static bool operator >=(WorkChapter left, Work right)
        {
            return !left.LessThan(right);
        }
        public static bool operator <=(WorkChapter left, Work right)
        {
            return left.LessThanOrEqual(right);
        }
        public static bool operator >=(Work left, IWorkChapter right)
        {
            return !(left as IWorkChapter).LessThan(right);
        }
        public static bool operator <=(Work left, IWorkChapter right)
        {
            return (left as IWorkChapter).LessThanOrEqual(right);
        }
        public static bool operator >=(IWorkChapter left, Work right)
        {
            return (right as IWorkChapter).LessThanOrEqual(left);
        }
        public static bool operator <=(IWorkChapter left, Work right)
        {
            return !(right as IWorkChapter).LessThan(left);
        }
    }
}

