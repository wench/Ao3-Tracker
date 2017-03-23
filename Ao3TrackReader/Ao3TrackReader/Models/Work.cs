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

using System;
using SQLite;
using Ao3TrackReader.Helper;

namespace Ao3TrackReader
{
    public sealed class Work : IWorkChapterEx
    {
        public Work()
        {
        }

        [PrimaryKey, Column("id")]
        public long workid { get; set; }
        [Column("chapterid")]
        public long chapterid { get; set; }
        [Column("number")]
        public long number { get; set; }
        [Column("timestamp")]
        public long Timestamp { get; set; }
        [Column("location")]
        public long? location { get; set; }
        [Column("seq")]
        public long Seq { get; set; }

        [Ignore]
        long? IWorkChapter.seq { get { return Seq; } set { Seq = value ?? 0; } }

        [Ignore]
        public long paragraph
        {
            get
            {
                if (location == null) return long.MaxValue;
                return (long)location / 1000000000L;
            }
        }

        [Ignore]
        public long frac
        {
            get
            {
                if (location == null) return long.MaxValue;
                var offset = (long)location % 1000000000L;
                if (offset == 500000000L) return 100;
                return offset * 100L / 479001600L;
            }
        }

        public bool LessThan(Work newitem)
        {
            if (newitem.workid != workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.Seq > this.Seq) { return true; }
            else if (newitem.Seq < this.Seq) { return false; }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (this.location == null) { return false; }
            if (newitem.location == null) { return true; }

            return newitem.location > this.location;
        }
        public bool LessThan(IWorkChapter newitem)
        {
            if (newitem is IWorkChapterEx) return LessThan(newitem as IWorkChapterEx);

            if (newitem.seq > this.Seq) { return true; }
            else if (newitem.seq < this.Seq) { return false; }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (this.location == null) { return false; }
            if (newitem.location == null) { return true; }

            return newitem.location > this.location;
        }
        public bool LessThan(IWorkChapterEx newitem)
        {
            if (newitem.workid != workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.seq > this.Seq) { return true; }
            else if (newitem.seq < this.Seq) { return false; }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (this.location == null) { return false; }
            if (newitem.location == null) { return true; }

            return newitem.location > this.location;
        }
        public bool LessThanOrEqual(Work newitem)
        {
            if (newitem.workid != workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.Seq > this.Seq) { return true; }
            else if (newitem.Seq < this.Seq) { return false; }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (newitem.location == null) { return true; }
            if (this.location == null) { return false; }

            return newitem.location >= this.location;
        }

        public bool LessThanOrEqual(IWorkChapter newitem)
        {
            if (newitem is IWorkChapterEx) return LessThanOrEqual(newitem as IWorkChapterEx);

            if (newitem.seq > this.Seq) { return true; }
            else if (newitem.seq < this.Seq) { return false; }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (newitem.location == null) { return true; }
            if (this.location == null) { return false; }

            return newitem.location >= this.location;
        }
        public bool LessThanOrEqual(IWorkChapterEx newitem)
        {
            if (newitem.workid != workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.seq > this.Seq) { return true; }
            else if (newitem.seq < this.Seq) { return false; }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (newitem.location == null) { return true; }
            if (this.location == null) { return false; }

            return newitem.location >= this.location;
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

