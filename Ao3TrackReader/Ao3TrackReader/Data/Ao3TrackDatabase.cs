using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;

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
                return database.Table<Work>().FirstOrDefault(x => x.id == id);
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
                    var row = database.Table<Work>().FirstOrDefault(x => x.id == item);
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

                    var row = database.Table<Work>().FirstOrDefault(x => x.id == item.id);
                    if (row != null)
                    {
                        if (row.IsNewer(item)) { 
                            database.Update(item);
                            newitems[item.id] = item;
                        }
                    }
                    else
                    {
                        database.Insert(item);
                        newitems[item.id] = item;
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

        public string GetVariable(string name)
        {
            lock (locker)
            {
                return (from x in database.Table<Variable>() where x.name == name select x.name).FirstOrDefault();
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
            }
        }
    }
}
