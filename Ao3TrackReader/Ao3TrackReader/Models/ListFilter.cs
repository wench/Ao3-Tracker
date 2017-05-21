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
    public enum ListFilterType
    {
        Tag = 0,        // <<unescaped tag>>
        Author = 1,     // <<user name>>
        Work = 2,       // <<work id>> <<Work Name>> - Name is ignored
        Series = 3      // <<series id>> <<Series Name>> - Name is ignored
    }

    public class ServerListFilters
    {
        public long last_sync { get; set; } = 0;
        public Dictionary<string, long> filters { get; set; }
    }

    public class ListFilter
    {
        [PrimaryKey]
        public string data { get; set; }
        public long timestamp { get; set; }
    }
}
