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
    public class ServerListFilters
    {
        public long last_sync { get; set; } = 0;
        public Dictionary<string, long> filters { get; set; }
    }

    public class ListFilter : ICachedTimestampedTableRow<string>, IEquatable<ListFilter>
    {
        [PrimaryKey]
        public string data { get; set; }
        public long timestamp { get; set; }

        string ICachedTableRow<string>.Primarykey => data;

        long ICachedTimestampedTableRow<string>.Timestamp { get => timestamp; set => timestamp = value; }

        #region Equality checks and Hashing
        public bool Equals(ListFilter other)
        {
            if (ReferenceEquals(this, other)) return true;
            return !(other is null) && data == other.data && timestamp == other.timestamp;
        }

        public override bool Equals(object obj)
        {
            return (obj is ListFilter other) && Equals(other);
        }

        public static bool operator ==(ListFilter left, ListFilter right)
        {
            if ((left is null) && (right is null)) return true;
            else if (left is null) return false;
            else if (right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(ListFilter left, ListFilter right)
        {
            if ((left is null) && (right is null)) return false;
            else if (left is null) return true;
            else if (right is null) return true;
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            return data.GetHashCodeSafe() ^
                timestamp.GetHashCode();
        }
        #endregion
    }
}
