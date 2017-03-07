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
