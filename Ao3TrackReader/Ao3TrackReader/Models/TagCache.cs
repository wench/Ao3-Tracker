using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace Ao3TrackReader.Models
{
    public class TagCache
    {
        public TagCache()
        {
            parents = new List<string>();
        }

        [PrimaryKey, Column("name")]
        public string name { get; set; }

        [Column("id")]
        public int id { get; set; }

        [Column("actual")]
        public string actual { get; set; }

        [Column("category")]
        public string category { get; set; }

        [Column("expires")]
        public DateTime expires { get; set; }


        [Column("parents")]
        public string parentsStr
        {
            get
            {
                return string.Join(",", parents);
            }
            set
            {
                parents = new List<string>(value?.Split(',') ?? new string[0]);
            }
        }

        [Ignore]
        public List<string> parents { get; set; }
    }
}
