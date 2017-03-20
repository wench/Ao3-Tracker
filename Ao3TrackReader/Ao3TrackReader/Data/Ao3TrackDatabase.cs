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

namespace Ao3TrackReader
{
	public class Ao3TrackDatabase
	{
		static object locker = new object ();   

		SQLiteConnection database;
		string DatabasePath {
			get { 
				var sqliteFilename = "Ao3TrackSQLite.db3";
				#if __IOS__
				string documentsPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal); // Documents folder
				string libraryPath = Path.Combine (documentsPath, "..", "Library"); // Library folder
				var path = Path.Combine(libraryPath, sqliteFilename);
				#else
				#if __ANDROID__
				string documentsPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal); // Documents folder
				var path = Path.Combine(documentsPath, sqliteFilename);
				#else
				// WinPhone
				var path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, sqliteFilename);;
				#endif
				#endif
				return path;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Tasky.DL.TaskDatabase"/> TaskDatabase. 
		/// if the database doesn't exist, it will create the database and all the tables.
		/// </summary>
		/// <param name='path'>
		/// Path.
		/// </param>
		public Ao3TrackDatabase()
		{
			database = new SQLiteConnection (DatabasePath);
            // create the tables
            database.CreateTable<Work>();
			database.CreateTable<Variable>();
            database.CreateTable<TagCache>();
            database.CreateTable<LanguageCache>();
            database.CreateTable<ReadingList>();
            database.CreateTable<SortColumn>();

            database.Query<Work>("UPDATE Work SET seq=0 WHERE seq IS NULL");
        }

		public IEnumerable<Work> GetItems ()
		{
			lock (locker) {
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
					if (row != null)
					{
						result[item] = row;
					}
				}

				return result.Values;
			}
		}

		public IReadOnlyCollection<Work> SaveItems (params Work[] items) 
		{
			return SaveItems(items as IReadOnlyCollection<Work>);

		}

		public IReadOnlyCollection<Work> SaveItems(IReadOnlyCollection<Work> items)
		{
			lock (locker)
			{
				Dictionary<long,Work> newitems = new Dictionary<long, Work>();

				foreach (var item in items) {

					var row = database.Table<Work>().FirstOrDefault(x => x.workid == item.workid);
					if (row != null)
					{
						if (row.LessThan(item)) { 
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

				return newitems.Values;
			}
		}

		public int DeleteItem(int id)
		{
			lock (locker) {
				return database.Delete<Work>(id);
			}
		}

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

        public string GetVariable(string name)
		{
			lock (locker)
			{
				var row = database.Table<Variable>().FirstOrDefault(x => x.name == name);
				if (row != null)
				{
					return row.value;
				}
				else
				{
					return null;
				}
			}
		}

        public delegate bool TryParseDelegate<T>(string s, out T result);

        public bool TryGetVariable<T>(string name, TryParseDelegate<T> tryparse, out T result, T onFailure = default(T))
        {
            lock (locker)
            {
                var row = database.Table<Variable>().FirstOrDefault(x => x.name == name);
                if (row != null)
                {
                    if (row.value != null && tryparse(row.value, out result))
                        return true;

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
                if (row != null)
                {
                    if (row.value != null) 
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
                result = onFailure;
                return false;
            }
        }

        public void SaveVariable(string name, string value)
        {
            lock (locker)
            {
                var row = database.Table<Variable>().FirstOrDefault(x => x.name == name);
                if (row != null)
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

        public void SaveVariable<T>(string name, T value)
        {
            SaveVariable(name, value.ToString());
        }

        public void SaveVariable<T>(string name, T? value)
            where T : struct
        {
            SaveVariable(name, value.HasValue ? value.ToString() : null);
        }

        public void DeleteVariable(string name)
        {
            lock (locker)
            {
                database.Delete<Variable>(name);
                if (variableEvents.TryGetValue(name, out var varEvents)) varEvents.OnDeleted(this, name);
            }
        }

        public string GetTag(int id)
		{
			lock (locker)
			{
				var now = DateTime.UtcNow;
				var row = database.Table<TagCache>().FirstOrDefault(x => x.id == id && x.expires > now);
				return row?.name;
			}
		}

		public TagCache GetTag(string name)
		{
			lock (locker)
			{
				var now = DateTime.UtcNow;
				var row = database.Table<TagCache>().FirstOrDefault(x => x.name == name && x.expires > now);
				return row;
			}

		}

		public void SetTagId(string name, int id)
		{
			lock (locker)
			{
				var now = DateTime.UtcNow;
				var expires = now + new TimeSpan(7, 0, 0, 0);
				var row = database.Table<TagCache>().FirstOrDefault(x => x.name == name);
				if (row != null)
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
				tag.expires = now + new TimeSpan(7, 0, 0, 0);
				var row = database.Table<TagCache>().FirstOrDefault(x => x.name == tag.name);
				if (row != null)
				{
					if (row.expires <= now) tag.id = 0;
					database.Update(tag);
				}
				else
				{
					tag.id = 0;
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
                var row = database.Table<LanguageCache>().FirstOrDefault(x => x.name == name);
                if (row != null)
                {
                    row.id = id;
                    database.Update(row);
                }
                else
                {
                    database.Insert(new LanguageCache { name = name, id = id });
                }
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
                var row = database.Table<SortColumn>().FirstOrDefault(x => x.name == name);
                if (row != null)
                {
                    row.id = id;
                    database.Update(row);
                }
                else
                {
                    database.Insert(new SortColumn { name = name, id = id });
                }
            }

        }

        public IEnumerable<ReadingList> GetReadingListItems()
        {
            lock (locker)
            {
                return (from i in database.Table<ReadingList>() select i);
            }
        }

        public ReadingList GetReadingListItem(string uri)
        {
            lock (locker)
            {
                return database.Table<ReadingList>().FirstOrDefault(x => x.Uri == uri);
            }
        }

        public void SaveReadingListItems(params ReadingList[] items)
        {
            SaveReadingListItems(items as IReadOnlyCollection<ReadingList>);

        }

        public void SaveReadingListItems(IReadOnlyCollection<ReadingList> items)
        {
            lock (locker)
            {
                foreach (var item in items)
                {

                    var row = database.Table<ReadingList>().FirstOrDefault(x => x.Uri == item.Uri);
                    if (row != null)
                    {
                        if (item.Timestamp == 0) item.Timestamp = row.Timestamp;
                        database.Update(item);
                    }
                    else
                    {
                        database.Insert(item);
                    }
                }
            }
        }
        public void DeleteReadingListItems(params string[] items)
        {
            DeleteReadingListItems(items as IReadOnlyCollection<string>);

        }

        public void DeleteReadingListItems(IReadOnlyCollection<string> items)
        {
            lock (locker)
            {
                foreach (var item in items)
                {
                    database.Delete<ReadingList>(item);
                }
            }
        }
    }
}
