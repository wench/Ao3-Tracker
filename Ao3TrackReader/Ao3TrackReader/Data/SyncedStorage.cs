using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using Ao3TrackReader.Helper;

namespace Ao3TrackReader.Data
{
    public class SyncedStorage
    {
        static object locker = new object();

        Dictionary<long, Work> storage;
        Dictionary<long, Work> unsynced;

        enum SyncState
        {
            Disabled = -1,
            Syncing = 0,
            Ready = 1,
            Delayed = 2
        }

        SyncState serversync = SyncState.Syncing;
        long last_sync = 0;
        long no_sync_until = 0;
        CancellationTokenSource cts;
        const string url_base = "https://wenchy.net/ao3track/api";

        struct Authorization
        {
            public string username;
            public string credential;
            public string toBase64()
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(this.username + '\n' + this.credential));
            }
        };
        Authorization authorization;

        event EventHandler<bool> SyncFromServerEvent;

        public SyncedStorage()
        {
            storage = new Dictionary<long, Work>();
            unsynced = new Dictionary<long, Work>();

            var database = App.Database;

            try
            {
                last_sync = Convert.ToInt64(database.GetVariable("last_sync"));
            }
            catch
            {
                last_sync = 0;
            }

            authorization.username = database.GetVariable("authorization.username");
            authorization.credential = database.GetVariable("authorization.credential");

            foreach (var it in database.GetItems())
            {
                storage[it.id] = it;

                if (storage[it.id].timestamp > last_sync)
                {
                    unsynced[it.id] = storage[it.id];
                }
            }
            dosync();
        }

        void onSyncFromServer(bool success)
        {
            lock (locker)
            {
                var e = SyncFromServerEvent;
                SyncFromServerEvent = null;
                Task.Run(() =>
                {
                    e?.Invoke(this, success);
                });
            }
        }

        public void delayedsync(int timeout)
        {
            lock (locker)
            {
                //console.log("delayedsync: timeout = %i", timeout);
                var now = DateTime.UtcNow.Ticks;
                if (cts != null)
                {
                    //console.log("delayedsync: existing pending sync in %i", no_sync_until - now);
                    // If the pending sync is going to happen before timeout would elapse, just let it happen
                    if (no_sync_until <= now + timeout * 10000) { return; }
                    cts.Cancel();
                    cts = null;
                }
                //console.log("delayedsync: setting up timeout callback");
                no_sync_until = now + timeout * 10000;
                serversync = SyncState.Delayed;
                cts = new CancellationTokenSource();

                Task.Run(() =>
                {
                    if (cts.Token.WaitHandle.WaitOne(timeout))
                    {
                        cts = null;
                        return;
                    }
                    lock (locker)
                    {
                        cts = null;
                        dosync(true);
                    }
                });
            }

        }


        public void dosync(bool force = false)
        {
            lock (locker)
            {
                if (String.IsNullOrEmpty(authorization.username) || String.IsNullOrEmpty(authorization.credential))
                {
                    serversync = SyncState.Disabled;
                    onSyncFromServer(false);
                    //console.warn("dosync: FAILED. No credentials");
                    return;
                }
            }
            Task.Run(() =>
            {
                lock (locker)
                {

                }

            });
        }

        public void dosync(Action<bool> callback)
        {
            lock (locker)
            {
                if (serversync == SyncState.Disabled)
                {
                    Task.Run(() => callback(false));
                }
                else
                {
                    SyncFromServerEvent += (sender,success) => callback(success);
                    if (serversync != SyncState.Syncing) { dosync(true); }
                }

            }
        }
       
        public void getWorkChapters(ICollection<long> works, Action<IDictionary<long, IWorkChapter>> callback)
        {
            Task.Run(() =>
            {
                lock (locker)
                {
                    EventHandler<bool> sendResponse = (sender, success) =>
                    {
                        var r = new Dictionary<long, IWorkChapter>(works.Count);
                        foreach(long w in works)
                        {
                            Work work;
                            if (storage.TryGetValue(w,out work))
                            {
                                r[w] = work;
                            }
                            callback(r);
                        }
                    };
                    if (serversync == SyncState.Syncing)
                    {
                            SyncFromServerEvent += sendResponse;
                    }
                    else
                    { 
                        sendResponse(this,true);
                    }

                }
            });
        }

        public void setWorkChapters(IDictionary<long, IWorkChapter> works)
        {
            Task.Run(() =>
            {
                lock (locker)
                {
                    long time = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000;
                    Dictionary<long, Work> newitems = new Dictionary<long, Work>();
                    bool do_delayed = false;

                    foreach (var work in works)
                    {
                        Work old = null;
                        if (!storage.TryGetValue(work.Key, out old) || old.IsNewer(work.Value))
                        {
                            // Do a delayed since if we finished a chapter, or started a new one 
                            if (work.Value.location == null || work.Value.location == 0 || (old != null && work.Value.number > old.number))
                            {
                                do_delayed = true;
                            }
                            newitems[work.Key] = storage[work.Key] = new Work {
                                id = work.Key,
                                number = work.Value.number,
                                chapterid = work.Value.chapterid,
                                location = work.Value.location,
                                timestamp = time
                            };

                            unsynced[work.Key] = storage[work.Key];
                        }

                    }

                    if (newitems.Count > 0)
                    {
                        App.Database.SaveItems(newitems.Values);
                        
                        if (serversync == SyncState.Ready || serversync == SyncState.Delayed)
                        {
                            if (do_delayed)
                            {
                                delayedsync(20 * 1000);
                            }
                            else
                            {
                                dosync();
                            }
                        }
                    }
                }
            });
        }

        public Task<IDictionary<string,string>> Login(string username, string password)
        {
           return Task.Run(() =>
           {
               IDictionary<string, string> errors = null;

               return errors;
           });
        }
    }
}
