using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace Ao3TrackReader
{
    class Variable
    {
        [PrimaryKey]
        public string name { get; set; }
        public string value { get; set; }
    }
}
