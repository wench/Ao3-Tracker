using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ao3TrackReader.Data
{
    public interface ICachedTableRow<TKey>
        where TKey : IComparable<TKey>
    {
        TKey Primarykey { get; }
    }

    public interface ICachedTimestampedTableRow<TKey> : ICachedTableRow<TKey>
        where TKey : IComparable<TKey>
    {
        long Timestamp { get; set; }
    }


    public interface ICachedTableProvider<TRow, TKey>
        where TRow : class, ICachedTableRow<TKey>, IEquatable<TRow>
        where TKey : IComparable<TKey>
    {
        IEnumerable<TRow> Select();
        TRow Select(TKey key);
        void InsertOrUpdate(TRow item);
        void Delete(TKey key);
        AutoCommitTransaction DoTransaction();
        void SaveVariable(string name, string value);
    }


    public class CachedTable<TRow, TKey, TProvider>
        where TRow : class, ICachedTableRow<TKey>, IEquatable<TRow>
        where TKey : IComparable<TKey>
        where TProvider : ICachedTableProvider<TRow, TKey>
    {
        protected readonly TProvider provider;

        protected Dictionary<TKey, TRow> values = new Dictionary<TKey, TRow>();
        protected Dictionary<TKey, TRow> modified = new Dictionary<TKey, TRow>();
        protected SemaphoreSlim locker = new SemaphoreSlim(1);
        protected int deferring = 0;

        public CachedTable(TProvider provider)
        {
            this.provider = provider;
        }

        public async Task BeginDeferralAsync()
        {
            using (await locker.LockAsync())
            {
                deferring++;
            }
        }

        public async Task EndDeferralAsync()
        {
            using (await locker.LockAsync())
            {
                --deferring;
                if (deferring < 0)
                {
                    throw new Exception();
                }
                else if (deferring == 0 && (modified.Count != 0 || variables.Count != 0))
                {
                    var deleted = modified.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key);
                    var changed = modified.Where(kvp => kvp.Value != null);

                    using (provider.DoTransaction())
                    {
                        foreach (var key in deleted)
                        {
                            provider.Delete(key);
                        }
                        foreach (var kvp in changed)
                        {
                            provider.InsertOrUpdate(kvp.Value);
                        }
                        foreach (var kvp in variables)
                        {
                            provider.SaveVariable(kvp.Key, kvp.Value);
                        }
                    }

                    variables.Clear();
                    modified.Clear();
                }
            }
        }

        protected bool gotall = false;
        public async Task<ICollection<TRow>> SelectAsync()
        {
            using (await locker.LockAsync())
            {
                if (!gotall)
                {
                    foreach (var value in provider.Select())
                    {
                        if (!values.ContainsKey(value.Primarykey))
                            values[value.Primarykey] = value;
                    }
                    gotall = true;
                }
                return values.Values.Where(
                    (row) =>
                        row != null
                ).ToList();
            }
        }

        public async Task<TRow> SelectAsync(TKey key)
        {
            using (await locker.LockAsync())
            {
                if (!values.TryGetValue(key, out var value))
                    value = values[key] = provider.Select(key);

                return value;
            }
        }


        public virtual async Task InsertOrUpdateAsync(TRow item)
        {
            using (await locker.LockAsync())
            {
                TKey key = item.Primarykey;

                if (values.TryGetValue(key, out var existing) && item.Equals(existing))
                    return;

                if (deferring == 0)
                    provider.InsertOrUpdate(item);
                else
                    modified[item.Primarykey] = item;

                values[item.Primarykey] = item;
            }
        }


        public async Task DeleteAsync(TKey key)
        {
            using (await locker.LockAsync())
            {
                if (!values.TryGetValue(key, out var existing) || existing == null)
                    return;

                if (deferring == 0)
                    provider.Delete(key);
                else
                    modified[key] = null;

                values[key] = null;
            }
        }

        Dictionary<string, string> variables = new Dictionary<string, string>();
        public async Task SaveVariableAsync(string name, string value)
        {
            using (await locker.LockAsync())
            {
                if (deferring == 0)
                    provider.SaveVariable(name, value);
                else
                    variables[name] = value;
            }
        }

        public Task SaveVariableAsync(string name, object value)
        {
            return SaveVariableAsync(name, value?.ToString());
        }

        public Task SaveVariableAsync<T>(string name, T value)
        {
            return SaveVariableAsync(name, value?.ToString());
        }

        public Task SaveVariableAsync<T>(string name, T? value)
            where T : struct
        {
            return SaveVariableAsync(name, value?.ToString());
        }
    }

    public class CachedTimestampedTable<TRow, TKey, TProvider> : CachedTable<TRow,TKey,TProvider>
        where TRow : class, ICachedTimestampedTableRow<TKey>, IEquatable<TRow>
        where TKey : IComparable<TKey>
        where TProvider : ICachedTableProvider<TRow, TKey>
    {
        public CachedTimestampedTable(TProvider provider) : base(provider)
        {

        }

        public override async Task InsertOrUpdateAsync(TRow item)
        {
            using (await locker.LockAsync())
            {
                TKey key = item.Primarykey;

                if (!values.TryGetValue(key, out var existing))
                    existing = values[key] = provider.Select(key);

                if (item.Timestamp == 0 && existing != null)
                    item.Timestamp = existing.Timestamp;

                if (!item.Equals(existing))
                {
                    if (deferring == 0)
                        provider.InsertOrUpdate(item);
                    else
                        modified[key] = item;

                    values[key] = item;
                }
            }
        }
    }

}
