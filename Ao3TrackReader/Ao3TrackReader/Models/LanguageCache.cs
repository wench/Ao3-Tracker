using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Models
{
    class LanguageCache
    {
        [PrimaryKey]
        public int id { set; get; }

        public string name { set; get; }
    }
}
