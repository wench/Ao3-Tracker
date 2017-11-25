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
using System.IO;
using System.Linq;
using SQLite;
using Ao3TrackReader.Models;
using System.Threading;
using System.Threading.Tasks;
using Ao3TrackReader.Data;
using Ao3TrackReader.Helper;

namespace Ao3TrackReader
{
    public struct AutoCommitTransaction : IDisposable
    {
        private Ao3TrackDatabase db;

        public AutoCommitTransaction(Ao3TrackDatabase db)
        {
            this.db = db;
        }

        public void Dispose()
        {
            db.Commit();
        }
    }

    public class Ao3TrackDatabase : ICachedTableProvider<ReadingList, string>, ICachedTableProvider<ListFilter, string>
    {
        static object locker = new object();

        SQLiteConnection database;
        string DatabasePath
        {
            get
            {
                var sqliteFilename = "Ao3TrackSQLite.db3";
#if __IOS__
				string documentsPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal); // Documents folder
				string libraryPath = Path.Combine (documentsPath, "..", "Library"); // Library folder
				var path = Path.Combine(libraryPath, sqliteFilename);
#elif __ANDROID__
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // Documents folder
                var path = Path.Combine(documentsPath, sqliteFilename);
#elif __WINDOWS__
                // WinPhone
                var path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, sqliteFilename); ;
#endif
                return path;
            }
        }

        public Ao3TrackDatabase()
        {
            SQLitePCL.Batteries_V2.Init();
            SQLitePCL.raw.FreezeProvider();
            SQLitePCL.raw.sqlite3_shutdown();
            int result = SQLitePCL.raw.sqlite3_config(SQLitePCL.raw.SQLITE_CONFIG_SERIALIZED);
            database = new SQLiteConnection(DatabasePath);

            // create the tables
            database.CreateTable<Variable>();

            database.CreateTable<Work>();
            database.CreateTable<TagCache>();
            database.CreateTable<LanguageCache>();
            database.CreateTable<ReadingList>();
            database.CreateTable<SortColumn>();
            database.CreateTable<ListFilter>();

            // Do any database upgrades if necessaey
            TryGetVariable("DatabaseVersion", int.TryParse, out var DatabaseVersion, 0);

            if (DatabaseVersion < Version.Version.AsInteger(1, 0, 5, -1))
            {
                database.Execute("UPDATE Work SET seq=0 WHERE seq IS NULL");
                database.Execute("DELETE FROM TagCache"); // Delete the entire cache cause it's behaviour has slightly changed
            }
            if (DatabaseVersion < Version.Version.AsInteger(1, 0, 7, -1))
            {
                TryParseDelegate<bool?> tryparse = (string s, out bool? res) => { return TryParseNullable(s, out res, bool.TryParse); };
                var map = new NullKeyDictionary<bool?, UnitConvSetting>
                {
                    { null, UnitConvSetting.None },
                    { true, UnitConvSetting.USToMetric },
                    { false, UnitConvSetting.MetricToUS }
                };

                ConvertVariable("UnitConvOptions.tempToC", map, tryparse, "UnitConvOptions.temp");
                ConvertVariable("UnitConvOptions.distToM", map, tryparse, "UnitConvOptions.dist");
                ConvertVariable("UnitConvOptions.volumeToM", map, tryparse, "UnitConvOptions.volume");
                ConvertVariable("UnitConvOptions.weightToM", map, tryparse, "UnitConvOptions.weight");
            }
            if (DatabaseVersion < Version.Version.AsInteger(1,1,0,4))
            {
                try
                {
                    var oldValues = database.Query<ReadingListV1>("SELECT Uri, Timestamp, PrimaryTag, Title, Summary, Unread FROM ReadingList");
                    var newValues = new List<ReadingList>(oldValues.Count);
                    if (oldValues.Count > 0)
                    {
                        var models = Ao3SiteDataLookup.LookupQuick(oldValues.Select((row) => row.Uri));
                        foreach (var item in oldValues)
                        {
                            if (models.TryGetValue(item.Uri, out var model))
                            {
                                if (string.IsNullOrWhiteSpace(model.Title) || model.Type == Models.Ao3PageType.Work)
                                    model.Title = item.Title;
                                if (string.IsNullOrWhiteSpace(model.PrimaryTag) || model.PrimaryTag.StartsWith("<"))
                                {
                                    model.PrimaryTag = item.PrimaryTag;
                                    var tagdata = Ao3SiteDataLookup.LookupTagQuick(item.PrimaryTag);
                                    if (tagdata is null) model.PrimaryTagType = Models.Ao3TagType.Other;
                                    else model.PrimaryTagType = Ao3SiteDataLookup.GetTypeForCategory(tagdata.category);
                                }
                                if (!(model.Details is null) && (model.Details.Summary is null) && !string.IsNullOrEmpty(item.Summary))
                                    model.Details.Summary = item.Summary;

                                newValues.Add(new ReadingList(model, item.Timestamp, item.Unread));
                            }
                            else
                            {
                                newValues.Add(new ReadingList { Uri = item.Uri, Timestamp = item.Timestamp, Unread = item.Unread });
                            }

                        }

                    }
                    database.BeginTransaction();
                    database.DropTable<ReadingList>();
                    database.CreateTable<ReadingList>();
                    database.InsertAll(newValues, false);
                    database.Commit();
                }
                catch(Exception e)
                {

                }
            }

            if (DatabaseVersion != Version.Version.Integer) SaveVariable("DatabaseVersion", Version.Version.Integer);

            ReadingListCached = new CachedTimestampedTable<ReadingList, string, Ao3TrackDatabase>(this);
            ListFiltersCached = new CachedTimestampedTable<ListFilter, string, Ao3TrackDatabase>(this);
            // Work should be cached table too?
        }

        public IEnumerable<Work> GetItems()
        {
            lock (locker)
            {
                return (from i in database.Table<Work>() select i);
            }
        }

        public Work GetItem(long id)
        {
            lock (locker)
            {
                return database.Table<Work>().FirstOrDefault(x => x.workid == id);
            }
        }

        public IReadOnlyCollection<Work> GetItems(params long[] items)
        {
            return GetItems(items as IReadOnlyCollection<long>);
        }

        public IReadOnlyCollection<Work> GetItems(IReadOnlyCollection<long> items)
        {
            lock (locker)
            {
                Dictionary<long, Work> result = new Dictionary<long, Work>();

                foreach (var item in items)
                {
                    var row = database.Table<Work>().FirstOrDefault(x => x.workid == item);
                    if (!(row is null))
                    {
                        result[item] = row;
                    }
                }

                return result.Values.ToReadOnly();
            }
        }

        public IReadOnlyCollection<Work> SaveItems(params Work[] items)
        {
            return SaveItems(items as IReadOnlyCollection<Work>);

        }

        public IReadOnlyCollection<Work> SaveItems(IReadOnlyCollection<Work> items)
        {
            lock (locker)
            {
                Dictionary<long, Work> newitems = new Dictionary<long, Work>();

                foreach (var item in items)
                {

                    var row = database.Table<Work>().FirstOrDefault(x => x.workid == item.workid);
                    if (!(row is null))
                    {
                        if (row.LessThan(item))
                        {
                            database.Update(item);
                            newitems[item.workid] = item;
                        }
                    }
                    else
                    {
                        database.Insert(item);
                        newitems[item.workid] = item;
                    }
                }

                return newitems.Values.ToReadOnly();
            }
        }

        public int DeleteItem(int id)
        {
            lock (locker)
            {
                return database.Delete<Work>(id);
            }
        }
        #region Variables
        public class VariableEventArgs : EventArgs
        {
            public string VarName { get; set; }
        };

        public class VariableUpdatedEventArgs : VariableEventArgs
        {
            public string NewValue { get; set; }
            public string OldValue { get; set; }
        };


        public interface IVariableEvents
        {
            event EventHandler<VariableUpdatedEventArgs> Updated;
            event EventHandler<VariableEventArgs> Deleted;
        }

        class VariableEvents : IVariableEvents
        {
            public event EventHandler<VariableUpdatedEventArgs> Updated;
            public event EventHandler<VariableEventArgs> Deleted;

            public void OnUpdated(object sender, string name, string oldval, string newval)
            {
                if (oldval != newval)
                    Updated?.Invoke(sender, new VariableUpdatedEventArgs { VarName = name, OldValue = oldval, NewValue = newval });
            }
            public void OnDeleted(object sender, string name)
            {
                Deleted?.Invoke(sender, new VariableEventArgs { VarName = name });
            }
        }

        Dictionary<string, VariableEvents> variableEvents = new Dictionary<string, VariableEvents>();
        public IVariableEvents GetVariableEvents(string name)
        {
            lock (locker)
            {
                if (!variableEvents.TryGetValue(name, out var varEvents)) variableEvents[name] = varEvents = new VariableEvents();
                return varEvents;
            }
        }

        private Dictionary<string, object> variableDefaults = new Dictionary<string, object> {
            { "ToolbarBackBehaviour", WebViewPage.def_ToolbarBackBehaviour },
            { "ToolbarForwardBehaviour", WebViewPage.def_ToolbarForwardBehaviour },
            { "SwipeBackBehaviour", WebViewPage.def_SwipeBackBehaviour },
            { "SwipeForwardBehaviour", WebViewPage.def_SwipeForwardBehaviour },
            { "ListFiltering.OnlyGeneralTeen", ListFilteringSettings.def_onlyGeneralTeen },
            { "ListFiltering.HideWorks", false },
            { "Theme", "light" },
            { "LogFontSizeUI", 0 },
            { "UnitConvOptions.temp", UnitConvSetting.None },
            { "UnitConvOptions.dist", UnitConvSetting.None },
            { "UnitConvOptions.volume", UnitConvSetting.None },
            { "UnitConvOptions.weight", UnitConvSetting.None },
            { "TagOptions.showCatTags", false },
            { "TagOptions.showWIPTags", false },
            { "TagOptions.showRatingTags", false },
            { "ReadingList.showTagsDefault", false },
            { "ReadingList.showCompleteDefault", false }

        };

        public string GetVariable(string name)
        {
            lock (locker)
            {
                var row = database.Table<Variable>().FirstOrDefault(x => x.name == name);
                if (!(row?.value is null))
                {
                    return row.value;
                }
                else if (variableDefaults.TryGetValue(name, out var def))
                {
                    return def?.ToString();
                }
                return null;
            }
        }

        public delegate bool TryParseDelegate<T>(string s, out T result);

        public bool TryParseNullable<T>(string s, out T? result, TryParseDelegate<T> tryparse)
            where T : struct
        {
            if (s == null)
            {
                result = null;
                return true;
            }
            else if (tryparse(s, out T parsed))
            {
                result = parsed;
                return true;
            }
            result = null;
            return false;
        }

        public bool TryGetVariable<T>(string name, TryParseDelegate<T> tryparse, out T result, T onFailure = default(T))
        {
            lock (locker)
            {
                var row = database.Table<Variable>().FirstOrDefault(x => x.name == name);
                if (!(row?.value is null))
                {

                    if (tryparse(row.value, out result))
                        return true;

                }
                else if (variableDefaults.TryGetValue(name, out var def))
                {
                    if (def is T res)
                    {
                        result = res;
                        return true;
                    }
                }
                result = onFailure;
                return false;
            }
        }

        public bool TryGetVariable<T>(string name, TryParseDelegate<T> tryparse, out T? result, T? onFailure = null)
            where T : struct
        {
            lock (locker)
            {
                var row = database.Table<Variable>().FirstOrDefault(x => x.name == name);
                if (!(row is null))
                {
                    if (!(row.value is null))
                    {
                        if (tryparse(row.value, out T res))
                        {
                            result = res;
                            return true;
                        }
                    }
                    else
                    {
                        result = null;
                        return true;
                    }
                }
                if (variableDefaults.TryGetValue(name, out var def))
                {
                    if (def is T res)
                    {
                        result = res;
                        return true;
                    }
                }
                result = onFailure;
                return false;
            }
        }

        private void ConvertVariable<TSource, TDest>(string name, IDictionary<TSource, TDest> map, TryParseDelegate<TSource> tryparse, string newname = null)
        {
            if (string.IsNullOrEmpty(newname)) newname = name;

            if (TryGetVariable(name, tryparse, out var value))
            {
                if (map.TryGetValue(value, out var newvalue))
                {
                    SaveVariable(newname, newvalue);
                }

                if (name != newname)
                {
                    DeleteVariable(name);
                }
            }
        }


        public void SaveVariable(string name, string value)
        {
            lock (locker)
            {
                var row = database.Table<Variable>().FirstOrDefault(x => x.name == name);
                if (!(row is null))
                {
                    database.Update(new Variable { name = name, value = value });
                }
                else
                {
                    database.Insert(new Variable { name = name, value = value });
                }
                if (variableEvents.TryGetValue(name, out var varEvents)) varEvents.OnUpdated(this, name, row?.value, value);
            }
        }

        public void SaveVariable(string name, object value)
        {
            SaveVariable(name, value?.ToString());
        }

        public void SaveVariable<T>(string name, T value)
        {
            SaveVariable(name, value?.ToString());
        }

        public void SaveVariable<T>(string name, T? value)
            where T : struct
        {
            SaveVariable(name, value?.ToString());
        }

        public void DeleteVariable(string name)
        {
            lock (locker)
            {
                database.Delete<Variable>(name);
                if (variableEvents.TryGetValue(name, out var varEvents)) varEvents.OnDeleted(this, name);
            }
        }

        #endregion

        public string GetTag(int id)
        {
            lock (locker)
            {
                var now = DateTime.UtcNow;
                var row = database.Table<TagCache>().FirstOrDefault(x => x.id == id && x.expires > now);
                return row?.name;
            }
        }

        public TagCache GetTag(string name, bool ignoreexpires = false)
        {
            lock (locker)
            {
                if (!ignoreexpires)
                {
                    var now = DateTime.UtcNow;
                    var tag = database.Table<TagCache>().FirstOrDefault(x => x.name == name && x.expires > now);
                    if (tag?.actual != null) tag.actual = tag.actual.PoolString();
                    if (tag?.name != null) tag.name = tag.name.PoolString();
                    return tag;

                }
                else
                {
                    var tag = database.Table<TagCache>().FirstOrDefault(x => x.name == name);
                    if (tag?.actual != null) tag.actual = tag.actual.PoolString();
                    if (tag?.name != null) tag.name = tag.name.PoolString();
                    return tag;
                }
            }

        }

        Random random = new Random();

        DateTime RandomExpires()
        {
            return DateTime.UtcNow + TimeSpan.FromDays(21 + random.NextDouble() * 14);
        }

        bool intxn = false;
        DateTime txnExpires = DateTime.MinValue;

        DateTime TagExpires => intxn ? txnExpires : RandomExpires();

        public void SetTagId(string name, int id)
        {
            lock (locker)
            {
                var now = DateTime.UtcNow;
                var expires = TagExpires;
                var row = database.Table<TagCache>().FirstOrDefault(x => x.name == name);
                if (!(row is null))
                {
                    if (row.expires <= now) row = new TagCache { name = name };
                    row.id = id;
                    row.expires = expires;
                    database.Update(row);
                }
                else
                {
                    database.Insert(new TagCache { name = name, id = id, expires = expires });
                }
            }

        }

        public void SetTagDetails(TagCache tag)
        {
            lock (locker)
            {
                var now = DateTime.UtcNow;
                if (tag.actual != null) tag.actual = tag.actual.PoolString();
                if (tag.name != null) tag.name = tag.name.PoolString();
                tag.expires = TagExpires;
                var row = database.Table<TagCache>().FirstOrDefault(x => x.name == tag.name);
                if (!(row is null))
                {
                    tag.id = row.expires <= now ? 0 : row.id;
                    database.Update(tag);
                }
                else
                {
                    database.Insert(tag);
                }
            }
        }

        public string GetLanguage(int id)
        {
            lock (locker)
            {
                var row = database.Table<LanguageCache>().FirstOrDefault(x => x.id == id);
                return row?.name;
            }
        }

        public void SetLanguage(string name, int id)
        {
            lock (locker)
            {
                database.InsertOrReplace(new LanguageCache { name = name, id = id });
            }

        }

        public string GetSortColumn(string id)
        {
            lock (locker)
            {
                var row = database.Table<SortColumn>().FirstOrDefault(x => x.id == id);
                return row?.name;
            }
        }

        public void SetSortColumn(string name, string id)
        {
            lock (locker)
            {
                database.InsertOrReplace(new SortColumn { name = name, id = id });
            }

        }
        #region ReadingList
        public CachedTimestampedTable<ReadingList, string, Ao3TrackDatabase> ReadingListCached { get; }

        IEnumerable<ReadingList> ICachedTableProvider<ReadingList, string>.Select()
        {
            lock (locker)
            {
                return (from i in database.Table<ReadingList>() select i);
            }
        }

        ReadingList ICachedTableProvider<ReadingList, string>.Select(string uri)
        {
            lock (locker)
            {
                return database.Table<ReadingList>().FirstOrDefault(x => x.Uri == uri);
            }
        }

        void ICachedTableProvider<ReadingList, string>.InsertOrUpdate(ReadingList item)
        {
            lock (locker)
            {
                database.InsertOrReplace(item);
            }
        }

        void ICachedTableProvider<ReadingList, string>.Delete(string item)
        {
            lock (locker)
            {
                database.Delete<ReadingList>(item);
            }
        }
        #endregion

        #region ListFilters
        public CachedTimestampedTable<ListFilter, string, Ao3TrackDatabase> ListFiltersCached { get; }

        IEnumerable<ListFilter> ICachedTableProvider<ListFilter, string>.Select()
        {
            lock (locker)
            {
                return (from i in database.Table<ListFilter>() select i);
            }
        }

        ListFilter ICachedTableProvider<ListFilter, string>.Select(string data)
        {
            lock (locker)
            {
                return database.Table<ListFilter>().FirstOrDefault(x => x.data == data);
            }
        }

        void ICachedTableProvider<ListFilter, string>.InsertOrUpdate(ListFilter item)
        {
            lock (locker)
            {
                database.InsertOrReplace(item);
            }
        }

        void ICachedTableProvider<ListFilter, string>.Delete(string item)
        {
            lock (locker)
            {
                database.Delete<ListFilter>(item);
            }
        }
        #endregion

        #region Transactions    
        public AutoCommitTransaction DoTransaction()
        {
            Monitor.Enter(locker);
            intxn = true;
            txnExpires = RandomExpires();
            database.BeginTransaction();
            return new AutoCommitTransaction(this);
        }

        internal void Commit()
        {
            database.Commit();
            intxn = false;
            Monitor.Exit(locker);
        }
        #endregion
    }
}
