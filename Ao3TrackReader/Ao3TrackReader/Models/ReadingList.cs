using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace Ao3TrackReader.Models
{
    public class ServerReadingList
    {
        public long last_sync { get; set; }
        public Dictionary<string, long> paths { get; set; }
    }

    public class ReadingList
    {
        [PrimaryKey]
        public string Uri { get; set; }
        public string PrimaryTag { get; set; }
        public string Title { get; set; }
        public long Timestamp { get; set; }
    }
}
