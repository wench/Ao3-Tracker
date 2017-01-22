namespace Ao3tracksync.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Work
    {
        public Work()
        {
        }

        public Work(long userid, long id, long chapterid, long number, long? location, long timestamp, long seq)
        {
            this.userid = userid;
            this.id = id;
            this.chapterid = chapterid;
            this.number = number;
            this.location = location;
            this.timestamp = timestamp;
            this.seq = seq;
        }

    }
}