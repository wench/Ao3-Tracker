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

namespace Ao3TrackReader.Models
{

    public class KeyedItem<TKey, TValue>
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
            var o = obj as KeyedItem<TKey, TValue>;
            if (o == null) return false;
            return object.Equals(Key, o.Key) && object.Equals(Value, o.Value);
        }
    }

    public interface IKeyedItem
    {
        object Key { get; set; }
        string Value { get; set; }
    }

    public class KeyedItem<TKey> : KeyedItem<TKey, string>, IKeyedItem
    {
        public KeyedItem() { }
        public KeyedItem(TKey key, string value) : base(key, value)
        {
        }

        object IKeyedItem.Key { get => Key; set => Key = (TKey) value; }
        string IKeyedItem.Value { get => Value; set => Value = value; }
    }

    public class NullableKeyedItem<TKey> : KeyedItem<Nullable<TKey>>
        where TKey: struct
    {
        public NullableKeyedItem() { }
        public NullableKeyedItem(TKey key, string value) : base(key, value)
        {
        }
    }

    public class KeyedItemList<TKey, TValue> : ObservableCollection<KeyedItem<TKey, TValue>>
    {
        public KeyedItemList() : base()
        {

        }
        public KeyedItemList(IEnumerable<KeyedItem<TKey, TValue>> collection) : base(collection)
        {

        }

        public void Add(TKey key,TValue value)
        {
            Add(new KeyedItem<TKey, TValue>(key, value));
        }
    }

    public interface IKeyedItemList
    {
        IKeyedItem FindItem(string str);
    }

    public class KeyedItemList<TKey> : KeyedItemList<TKey,string>, IKeyedItemList
    {
        public KeyedItemList() : base()
        {
        }

        public KeyedItemList(IEnumerable<KeyedItem<TKey, string>> collection) : base(collection)
        {
        }

        Type typeKey;
        MethodInfo methodTryParse;

        IKeyedItem IKeyedItemList.FindItem(string str)
        {
            if (typeKey == null) typeKey = typeof(TKey);
            if (methodTryParse == null && !typeKey.GetTypeInfo().IsEnum)
            {
                if (typeKey == typeof(string))
                {
                    return (IKeyedItem)this.Find((TKey)(object)str);
                }
                methodTryParse = typeKey.GetMethod("TryParse", new[] { typeof(string), typeKey.MakeByRefType() });
            }

            if (str is null) return (IKeyedItem)this.Find(default(TKey));

            if (methodTryParse != null)
            {
                var parameters = new object[] { str, null };
                if ((bool)methodTryParse.Invoke(null, parameters))
                    return (IKeyedItem)this.Find((TKey)parameters[1]);
            }
            else if (typeKey.GetTypeInfo().IsEnum)
            {
                try
                {
                    var result = (TKey) Enum.Parse(typeKey, str);
                    return (IKeyedItem)this.Find(result);
                }
                catch
                {

                }
            }            

            return null;
        }
    }

    public class NullableKeyedItemList<TKey> : KeyedItemList<Nullable<TKey>>, IKeyedItemList
        where TKey: struct
    {
        Type typeKey;
        MethodInfo methodTryParse;

        IKeyedItem IKeyedItemList.FindItem(string str)
        {
            if (typeKey == null) typeKey = typeof(TKey);
            if (methodTryParse == null && !typeKey.GetTypeInfo().IsEnum)
                methodTryParse = typeKey.GetMethod("TryParse", new[] { typeof(string), typeKey.MakeByRefType() });

            if (str is null) return (IKeyedItem) this.Find((Nullable < TKey >) null);

            if (methodTryParse != null)
            {
                var parameters = new object[] { str, null };
                if ((bool) methodTryParse.Invoke(null, parameters))
                    return (IKeyedItem)this.Find((TKey) parameters[1]);
            }
            else if(typeKey.GetTypeInfo().IsEnum)
            {
                if (Enum.TryParse(str, out TKey result))
                    return (IKeyedItem)this.Find(result);
            }
            
            return null;
        }
    }

    public static class KeyedItemExtensions
    {
        public static KeyedItem<TKey> Find<TKey>(this IEnumerable<KeyedItem<TKey>> list, TKey key)
        {
            return list.FirstOrDefault((i) => Equals(i.Key, key));
        }
        public static KeyedItem<TKey, TValue> Find<TKey, TValue>(this IEnumerable<KeyedItem<TKey, TValue>> list, TKey key)
        {
            return list.FirstOrDefault((i) => Equals(i.Key, key));
        }
    }

}
