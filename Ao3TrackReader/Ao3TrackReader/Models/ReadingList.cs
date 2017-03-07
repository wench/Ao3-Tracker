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
        public int? Unread { get; set; }
        public string Summary { get; set; }
    }
}
