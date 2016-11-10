using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace Ao3TrackReader.Models
{
    public class TagCache
    {
        [PrimaryKey]
        public string name { get; set; }

        public int id { get; set; }

        public string merger { get; set; }

        public string category { get; set; }

        public long expires { get; set; }
    }
}
