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
using System.Collections.Generic;
using System.Text;


#if WINDOWS_UWP
using Windows.Foundation;
using Windows.Foundation.Metadata;
#endif

#pragma warning disable IDE1006

namespace Ao3TrackReader.Helper
{
    public interface IWorkChapter
    {
        long number { get; set; }
        long chapterid { get; set; }
        long? location { get; set; }
        long? seq { get; set; }
        bool LessThan(IWorkChapter newitem);
        bool LessThanOrEqual(IWorkChapter newitem);

        long paragraph { get; }
        long frac { get; }
    }
    public interface IWorkChapterEx : IWorkChapter
    {
        long workid { get; set; }
        bool LessThan(IWorkChapterEx newitem);
        bool LessThanOrEqual(IWorkChapterEx newitem);
    }

#if WINDOWS_UWP
    [AllowForWeb]
#endif
    public sealed class WorkChapter : IWorkChapterEx
    {
        public WorkChapter()
        {

        }

        public WorkChapter(IWorkChapterEx toCopy)
        {
            workid = toCopy.workid;
            number = toCopy.number;
            chapterid = toCopy.chapterid;
            location = toCopy.location;
            seq = toCopy.seq;
        }

        public long workid { get; set; }
        public long number { get; set; }
        public long chapterid { get; set; }
        public long? location { get; set; }
        public long? seq { get; set; }

        public long paragraph
        {
            get
            {
                if (location == null) return long.MaxValue;
                return (long)location / 1000000000L;
            }
        }
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

        bool IWorkChapter.LessThan(IWorkChapter newitem)
        {
            if (newitem is IWorkChapterEx) return LessThan(newitem as IWorkChapterEx);

            if (newitem.seq != null && this.seq != null)
            {
                if (newitem.seq > this.seq) { return true; }
                else if (newitem.seq < this.seq) { return false; }
            }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (this.location == null) { return false; }
            if (newitem.location == null) { return true; }

            return newitem.location > this.location;
        }
#if WINDOWS_UWP
        [DefaultOverload]
#endif
        public bool LessThan(IWorkChapterEx newitem)
        {
            if (newitem.workid != workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.seq != null && this.seq != null)
            {
                if (newitem.seq > this.seq) { return true; }
                else if (newitem.seq < this.seq) { return false; }
            }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (this.location == null) { return false; }
            if (newitem.location == null) { return true; }

            return newitem.location > this.location;
        }
        bool IWorkChapter.LessThanOrEqual(IWorkChapter newitem)
        {
            if (newitem is IWorkChapterEx) return LessThanOrEqual(newitem as IWorkChapterEx);

            if (newitem.seq != null && this.seq != null)
            {
                if (newitem.seq > this.seq) { return true; }
                else if (newitem.seq < this.seq) { return false; }
            }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (newitem.location == null) { return true; }
            if (this.location == null) { return false; }

            return newitem.location >= this.location;
        }
#if WINDOWS_UWP
        [DefaultOverload]
#endif
        public bool LessThanOrEqual(IWorkChapterEx newitem)
        {
            if (newitem.workid != workid) { throw new ArgumentException("Items don't belong to same work", "newitem"); }

            if (newitem.seq != null && this.seq != null)
            {
                if (newitem.seq > this.seq) { return true; }
                else if (newitem.seq < this.seq) { return false; }
            }

            if (newitem.number > this.number) { return true; }
            else if (newitem.number < this.number) { return false; }

            if (newitem.location == null) { return true; }
            if (this.location == null) { return false; }

            return newitem.location >= this.location;
        }

        //public static bool operator >=(WorkChapter left, WorkChapter right)
        //{
        //    return !left.LessThan(right);
        //}
        //public static bool operator <=(WorkChapter left, WorkChapter right)
        //{
        //    return left.LessThanOrEqual(right);
        //}
        //public static bool operator >=(WorkChapter left, IWorkChapter right)
        //{
        //    return !(left as IWorkChapter).LessThan(right);
        //}
        //public static bool operator <=(WorkChapter left, IWorkChapter right)
        //{
        //    return (left as IWorkChapter).LessThanOrEqual(right);
        //}
        //public static bool operator >=(IWorkChapter left, WorkChapter right)
        //{
        //    return (right as IWorkChapter).LessThanOrEqual(left);
        //}
        //public static bool operator <=(IWorkChapter left, WorkChapter right)
        //{
        //    return !(right as IWorkChapter).LessThan(left);
        //}
    }
}
