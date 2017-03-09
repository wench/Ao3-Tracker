using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Models
{
    public class KeyedItem<TKey,TValue>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }

        public KeyedItem() { }
        public KeyedItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public override string ToString()
        {
            return Value?.ToString() ?? "(null)";
        }

        public override int GetHashCode()
        {
            return Key?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            var o = obj as KeyedItem<TKey,TValue>;
            if (o == null) return false;
            return object.Equals(Key, o.Key) && object.Equals(Value, o.Value);
        }
    }
    public class KeyedItem<TKey> : KeyedItem<TKey, string>
    {
        public KeyedItem() { }
        public KeyedItem(TKey key, string value) : base(key, value)
        {
        }
    }
}
