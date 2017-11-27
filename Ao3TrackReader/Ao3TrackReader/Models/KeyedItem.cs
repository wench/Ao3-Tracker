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
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections;

namespace Ao3TrackReader.Models
{

    public interface IKeyedItem
    {
        object Key { get; set; }
        string Value { get; set; }
    }

    public class KeyedItem<TKey> : IKeyedItem
    {
        public TKey Key { get; set; }
        object IKeyedItem.Key { get => Key; set => Key = value is TKey ? (TKey)value : default(TKey); }

        public string Value { get; set; }

        public KeyedItem() { }
        public KeyedItem(TKey key, string value)
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
            var o = obj as KeyedItem<TKey>;
            if (o == null) return false;
            return object.Equals(Key, o.Key) && object.Equals(Value, o.Value);
        }

        public static explicit operator KeyValuePair<TKey, string>(KeyedItem<TKey> item)
        {
            return new KeyValuePair<TKey, string>(item.Key, item.Value);
        }
    }

    public class NullableKeyedItem<TKey> : KeyedItem<TKey?>
        where TKey: struct
    {
        public NullableKeyedItem() { }
        public NullableKeyedItem(TKey? key, string value)  : base(key,value)
        {
        }

        public TKey? KeyXaml
        {
            set => Key = value;
        }

        public string ValueXaml
        {
            set => Value = value;
        }

        public static NullableKeyedItem<TKey> Construct(TKey key, string value) { return new NullableKeyedItem<TKey>(key, value); }
        public static NullableKeyedItem<TKey> ConstructNull(string value) { return new NullableKeyedItem<TKey>(null, value); }
    }

    public interface IKeyedItemList
    {
        IKeyedItem Lookup(string str);
    }

    public abstract class BaseKeyedItemList<TKey> : List<KeyedItem<TKey>>, IKeyedItemList, IDictionary<TKey,string>
    {
        public IDictionary<TKey, string> Items => this;

        public BaseKeyedItemList() : base()
        {
        }

        public BaseKeyedItemList(IEnumerable<KeyedItem<TKey>> collection) : base(collection)
        {
        }


        string IDictionary<TKey, string>.this[TKey key] {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException(); }

        int ICollection<KeyValuePair<TKey, string>>.Count => Count;

        bool ICollection<KeyValuePair<TKey, string>>.IsReadOnly => false;

        public void Add(TKey key, string value)
        {
            base.Add(new KeyedItem<TKey>(key, value));
        }

        public new void Add(KeyedItem<TKey> item)
        {
            base.Add(item);
        }

        public abstract IKeyedItem Lookup(string str);


        #region IDictionary Stuff

        abstract class EnumeratorWrapper<T> : IEnumerator<T>
        {
            protected IEnumerator<KeyedItem<TKey>> enumerator { get; }
            public abstract T Current { get; }

            object IEnumerator.Current => Current;
            protected EnumeratorWrapper(IEnumerator<KeyedItem<TKey>> enumerator) { this.enumerator = enumerator; }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }

            public void Dispose()
            {
                enumerator.Dispose();
            }
        }

        abstract class CollectionWrapper<T> : ICollection<T>
        {
            protected BaseKeyedItemList<TKey> self { get; }

            protected CollectionWrapper(BaseKeyedItemList<TKey> self) { this.self = self; }

            public bool IsReadOnly => true;

            public int Count => ((ICollection)self).Count;

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public void Add(T item)
            {
                throw new NotSupportedException();
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public abstract IEnumerator<T> GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }

            public abstract bool Contains(T item);
        }

        void ICollection<KeyValuePair<TKey, string>>.Add(KeyValuePair<TKey, string> item)
        {
            base.Add(new KeyedItem<TKey>(item.Key, item.Value));
        }

        void ICollection<KeyValuePair<TKey, string>>.Clear()
        {
            Clear();
        }

        bool ICollection<KeyValuePair<TKey, string>>.Contains(KeyValuePair<TKey, string> item)
        {
            return Exists((i) => object.Equals(i.Key, item.Key) && i.Value == item.Value);
        }

        bool IDictionary<TKey, string>.ContainsKey(TKey key)
        {
            return Exists((i) => object.Equals(i.Key, key));
        }

        void ICollection<KeyValuePair<TKey, string>>.CopyTo(KeyValuePair<TKey, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        class KVPEnumeratorWrapper : EnumeratorWrapper<KeyValuePair<TKey, string>>, IEnumerator<KeyValuePair<TKey, string>>
        {
            public KVPEnumeratorWrapper(IEnumerator<KeyedItem<TKey>> enumerator) : base(enumerator) { }

            public override KeyValuePair<TKey, string> Current => (KeyValuePair<TKey, string>)enumerator.Current;
        }

        IEnumerator<KeyValuePair<TKey, string>> IEnumerable<KeyValuePair<TKey, string>>.GetEnumerator()
        {
            return new KVPEnumeratorWrapper(this.GetEnumerator());
        }

        bool IDictionary<TKey, string>.Remove(TKey key)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<TKey, string>>.Remove(KeyValuePair<TKey, string> item)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<TKey, string>.TryGetValue(TKey key, out string value)
        {
            throw new NotImplementedException();
        }

        class KeyEnumeratorWrapper : EnumeratorWrapper<TKey>
        {
            public KeyEnumeratorWrapper(IEnumerator<KeyedItem<TKey>> enumerator) : base(enumerator) { }

            public override TKey Current => enumerator.Current.Key;
        }

        class KeyCollectionWrapper : CollectionWrapper<TKey>
        {
            public KeyCollectionWrapper(BaseKeyedItemList<TKey> self) : base(self) { }

            public override bool Contains(TKey item)
            {
                return self.Exists((i) => object.Equals(i.Key, item));
            }

            public override IEnumerator<TKey> GetEnumerator()
            {
                return new KeyEnumeratorWrapper(self.GetEnumerator());
            }
        }

        ICollection<TKey> IDictionary<TKey, string>.Keys => new KeyCollectionWrapper(this);

        class ValueEnumeratorWrapper : EnumeratorWrapper<string>
        {
            public ValueEnumeratorWrapper(IEnumerator<KeyedItem<TKey>> enumerator) : base(enumerator) { }

            public override string Current => enumerator.Current.Value;
        }

        class ValueCollectionWrapper : CollectionWrapper<string>
        {
            public ValueCollectionWrapper(BaseKeyedItemList<TKey> self) : base(self) { }

            public override bool Contains(string item)
            {
                return self.Exists((i) => object.Equals(i.Value, item));
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return new ValueEnumeratorWrapper(self.GetEnumerator());
            }
        }
        ICollection<string> IDictionary<TKey, string>.Values => new ValueCollectionWrapper(this);
        #endregion
    }


    public class KeyedItemList<TKey> : BaseKeyedItemList<TKey>
    {
        static Type typeKey;
        static TypeInfo typeInfoKey;
        static MethodInfo methodTryParse;
        
        static TKey ResolveKey(string str)
        {
            if (typeKey == null)
            {
                typeKey = typeof(TKey);
                typeInfoKey = typeKey.GetTypeInfo();
            }
            if (methodTryParse == null && !typeInfoKey.IsEnum)
            {
                if (typeKey == typeof(string))
                {
                    return (TKey)(object)str;
                }
                methodTryParse = typeKey.GetRuntimeMethod("TryParse", new[] { typeof(string), typeKey.MakeByRefType() });
            }

            if ((str is null) && typeInfoKey.IsClass || (typeInfoKey.IsGenericType && typeInfoKey.GetGenericTypeDefinition() == typeof(Nullable)))
                return default(TKey);

            if (methodTryParse != null)
            {
                var parameters = new object[] { str, null };
                if ((bool)methodTryParse.Invoke(null, parameters))
                    return (TKey)parameters[1];
            }
            else if (typeKey.GetTypeInfo().IsEnum)
            {
                try
                {
                    return (TKey)Enum.Parse(typeKey, str);
                }
                catch
                {

                }
            }

            throw new ArgumentException();
        }

        public override IKeyedItem Lookup(string strKey)
        {
            try
            {
                return this.Find(ResolveKey(strKey));
            }
            catch
            {
                return null;
            }
        }
    }

    public class NullableKeyedItemList<TKey> : BaseKeyedItemList<TKey?>
        where TKey: struct
    {
        static Type typeKey;
        static TypeInfo typeInfoKey;
        static MethodInfo methodTryParse;

        static TKey? ResolveKey(string str)
        {
            if (typeKey == null)
            {
                typeKey = typeof(TKey);
                typeInfoKey = typeKey.GetTypeInfo();
            }
            if (methodTryParse == null && !typeInfoKey.IsEnum)
                methodTryParse = typeKey.GetRuntimeMethod("TryParse", new[] { typeof(string), typeKey.MakeByRefType() });

            if (str is null) return null;

            if (methodTryParse != null)
            {
                var parameters = new object[] { str, null };
                if ((bool) methodTryParse.Invoke(null, parameters))
                    return (TKey)parameters[1];
            }
            else if(typeKey.GetTypeInfo().IsEnum)
            {
                if (Enum.TryParse(str, out TKey result))
                    return result;
            }

            throw new ArgumentException();
        }

        public override IKeyedItem Lookup(string str)
        {
            try
            {
                return this.Find(ResolveKey(str));
            }
            catch
            {
                return null;
            }
        }

        public void Add(TKey key, string value)
        {
            base.Add(new KeyedItem<TKey?>(key, value));
        }

    }

    public static class KeyedItemExtensions
    {
        public static KeyedItem<TKey> Find<TKey>(this IEnumerable<KeyedItem<TKey>> list, TKey key)
        {
            return list.FirstOrDefault((i) => Equals(i.Key, key));
        }
    }
}
