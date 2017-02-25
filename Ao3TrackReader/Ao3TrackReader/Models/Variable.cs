using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace Ao3TrackReader
{
    class Variable
    {
        [PrimaryKey, Column("name")]
        public string name { get; set; }
        [Column("value")]
        public string value { get; set; }
    }
}
