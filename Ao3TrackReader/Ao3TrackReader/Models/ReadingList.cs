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
using Ao3TrackReader.Data;

namespace Ao3TrackReader.Models
{
    public class ServerReadingList
    {
        public long last_sync { get; set; } = 0;
        public Dictionary<string, long> paths { get; set; }
    }

    class ReadingListV1
    {
        public string Uri { get; set; }
        public long Timestamp { get; set; }
        public string PrimaryTag { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public int? Unread { get; set; }
    }

    public class ReadingList : ICachedTimestampedTableRow<string>, IEquatable<ReadingList>
    {
        public ReadingList() { }
        public ReadingList(ReadingList copy)
        {
            Uri = copy.Uri;
            Timestamp = copy.Timestamp;
            Unread = copy.Unread;
            Model = copy.Model;
        }
        public ReadingList(Ao3PageModel model, long timestamp, int? unread)
        {
            Uri = model.Uri.AbsoluteUri;
            Timestamp = timestamp;
            Unread = unread;
            Model = Ao3PageModel.Serialize(model);
        }

        [PrimaryKey]
        public string Uri { get; set; }
        public long Timestamp { get; set; }
        public int? Unread { get; set; }
        public bool Favourite { get; set; }
        public string Model { get; set; }

        string ICachedTableRow<string>.Primarykey => Uri; 

        #region Equality checks and Hashing
        public bool Equals(ReadingList other)
        {
            if (ReferenceEquals(this, other)) return true;
            return !(other is null) && Uri == other.Uri &&
                Timestamp == other.Timestamp &&
                Unread == other.Unread &&
                Model == other.Model;
        }

        public override bool Equals(object obj)
        {
            return (obj is ReadingList other) && Equals(other);
        }            

        public static bool operator ==(ReadingList left, ReadingList right)
        {
            if ((left is null) && (right is null)) return true;
            else if (left is null) return false;
            else if (right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(ReadingList left, ReadingList right)
        {
            if ((left is null) && (right is null)) return false;
            else if (left is null) return true;
            else if (right is null) return true;
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            return Uri.GetHashCodeSafe() ^
                Timestamp.GetHashCode() ^
                Unread.GetHashCode() ^
                Model.GetHashCodeSafe();
        }
        #endregion
    }
}
