using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ao3TrackReader.Models
{
    public class NullKeyDictionary<TKey,TValue> : IDictionary<TKey,TValue>
    {
        Dictionary<TKey, TValue> storage;
        bool hasNull = false;
        TValue nullValue;

        public NullKeyDictionary()
        {
            storage = new Dictionary<TKey, TValue>();
        }

        public NullKeyDictionary(int capacity)
        {
            storage = new Dictionary<TKey, TValue>(capacity);
        }

        public TValue this[TKey key] {
            get {
                if ((object)key is null)
                {
                    if (!hasNull) throw new KeyNotFoundException();
                    return nullValue;
                }
                return ((IDictionary<TKey, TValue>)storage)[key];
            }
            set {
                if ((object)key is null)
                {
                    hasNull = true;
                    nullValue = value;
                }
                else
                {
                    ((IDictionary<TKey, TValue>)storage)[key] = value;
                }
            }
        }

        public ICollection<TKey> Keys
        {
            get {
                if (hasNull) {
                    var res = new TKey[storage.Keys.Count + 1];
                    storage.Keys.CopyTo(res, 0);
                    res[storage.Keys.Count] = (TKey)(object)null;
                    return res;
                }
                return storage.Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                if (hasNull)
                {
                    var res = new TValue[storage.Values.Count + 1];
                    storage.Values.CopyTo(res, 0);
                    res[storage.Keys.Count] = nullValue;
                    return res;
                }
                else
                {
                    return storage.Values;
                }
            }
        }

        public int Count => ((IDictionary<TKey, TValue>)storage).Count + (hasNull?1:0);

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            if ((object) key is null)
            {
                if (hasNull) throw new ArgumentException();
                hasNull = true;
                nullValue = value;
                return;
            }
            ((IDictionary<TKey, TValue>)storage).Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            if ((object)item.Key is null)
            {
                if (hasNull) throw new ArgumentException();
                hasNull = true;
                nullValue = item.Value;
                return;
            }

            ((IDictionary<TKey, TValue>)storage).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<TKey, TValue>)storage).Clear();
            hasNull = false;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if ((object)item.Key is null)
                return hasNull;

            return ((IDictionary<TKey, TValue>)storage).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            if ((object)key is null)
                return hasNull;

            return ((IDictionary<TKey, TValue>)storage).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (hasNull && array.Length < arrayIndex + storage.Count + 1)
                throw new ArgumentException();
            ((IDictionary<TKey, TValue>)storage).CopyTo(array, arrayIndex);
            if (hasNull) array[arrayIndex + storage.Count] = new KeyValuePair<TKey,TValue>((TKey)(object)null,nullValue);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (hasNull) {
                return (storage.Concat(new[] { new KeyValuePair<TKey, TValue>((TKey)(object)null, nullValue) })).GetEnumerator();
            }

            return ((IDictionary<TKey, TValue>)storage).GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            if ((object)key is null)
            {
                bool had = hasNull;
                hasNull = false;
                return had;
            }
            return ((IDictionary<TKey, TValue>)storage).Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if ((object)item.Key is null)
            {
                bool had = hasNull;
                hasNull = false;
                return had;
            }
            return ((IDictionary<TKey, TValue>)storage).Remove(item);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if ((object)key is null)
            {
                if (hasNull)
                {
                    value = nullValue;
                    return true;
                }
                value = default(TValue);
                return false;
            }
            return ((IDictionary<TKey, TValue>)storage).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
