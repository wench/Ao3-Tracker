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

    public interface IKeyedItem
    {
        object Key { get; set; }
        string Value { get; set; }
    }

    public class KeyedItem<TKey> : IKeyedItem
    {
        public TKey Key { get; set; }
        object IKeyedItem.Key { get => Key; set => Key = (TKey)value; }

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

    }

    public class NullableKeyedItem<TKey> : KeyedItem<TKey?>
        where TKey: struct
    {
        public NullableKeyedItem() { }
        public NullableKeyedItem(TKey key, string value) : base(key, value)
        {
        }
    }

    public interface IKeyedItemList
    {
        IKeyedItem Lookup(string str);
    }

    public abstract class BaseKeyedItemList<TKey> : List<KeyedItem<TKey>>, IKeyedItemList
    {
        public BaseKeyedItemList() : base()
        {
        }

        public BaseKeyedItemList(IEnumerable<KeyedItem<TKey>> collection) : base(collection)
        {
        }

        public void Add(TKey key, string value)
        {
            Add(new KeyedItem<TKey>(key, value));
        }

        public abstract IKeyedItem Lookup(string str);
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
                methodTryParse = typeKey.GetMethod("TryParse", new[] { typeof(string), typeKey.MakeByRefType() });
            }

            if (str is null && typeInfoKey.IsClass || (typeInfoKey.IsGenericType && typeInfoKey.GetGenericTypeDefinition() == typeof(Nullable)))
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
                methodTryParse = typeKey.GetMethod("TryParse", new[] { typeof(string), typeKey.MakeByRefType() });

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
    }

    public static class KeyedItemExtensions
    {
        public static KeyedItem<TKey> Find<TKey>(this IEnumerable<KeyedItem<TKey>> list, TKey key)
        {
            return list.FirstOrDefault((i) => Equals(i.Key, key));
        }
    }
}
