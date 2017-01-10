using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
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

        SyncState serversync = SyncState.Disabled;
        long last_sync = 0;
        long no_sync_until = 0;
        CancellationTokenSource cts;
        readonly Uri url_base = new Uri("https://wenchy.net/ao3track/api/");

        struct Authorization
        {
            public string username;
            public string credential;
            public string toBase64()
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(this.username + '\n' + this.credential));
            }
        };

        public event EventHandler BeginSyncEvent;
        public event EventHandler<bool> EndSyncEvent;

        public bool IsSyncing
        {
            get { return serversync == SyncState.Syncing; }
        }

        public bool CanSync
        {
            get { return serversync != SyncState.Delayed; }
        }

        public string Username
        {
            get { return App.Database.GetVariable("authorization.username") ?? ""; }
        }


        event EventHandler<bool> SyncFromServerEvent;

        HttpClient HttpClient { get; set; }

        public SyncedStorage()
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            HttpClient = new HttpClient(httpClientHandler);

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

            Authorization authorization;
            authorization.username = database.GetVariable("authorization.username") ?? "";
            authorization.credential = database.GetVariable("authorization.credential") ?? "";
            if (String.IsNullOrEmpty(authorization.username) || String.IsNullOrEmpty(authorization.credential))
            {
                serversync = SyncState.Disabled;
            }
            else
            {
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Ao3track", authorization.toBase64());
                serversync = SyncState.Syncing;
            }

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
                EndSyncEvent?.Invoke(this, success);
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
                var now = DateTime.UtcNow.Ticks / 10000;
                if (cts != null)
                {
                    //console.log("delayedsync: existing pending sync in %i", no_sync_until - now);
                    // If the pending sync is going to happen before timeout would elapse, just let it happen
                    if (no_sync_until <= now + timeout) { return; }
                    cts.Cancel();
                    cts = null;
                }
                //console.log("delayedsync: setting up timeout callback");
                no_sync_until = now + timeout;
                serversync = SyncState.Delayed;
                var thiscts = cts = new CancellationTokenSource();

                Task.Run(() =>
                {
                    if (thiscts.Token.WaitHandle.WaitOne(timeout))
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
                if (serversync == SyncState.Disabled)
                {
                    onSyncFromServer(false);
                    //console.warn("dosync: FAILED. No credentials");
                    return;
                }

                // if we have on sync events, then do it now
                if (SyncFromServerEvent != null && SyncFromServerEvent.GetInvocationList().Length > 0)
                {
                    force = true;
                }

                // Enforce 5 minutes gap between server sync. Don't want to hammer the server while scrolling through a fic  
                var now = DateTime.UtcNow.Ticks / 10000;
                if (!force && now < no_sync_until)
                {
                    //console.log("dosync: have to wait %i for timeout", no_sync_until - now);
                    delayedsync((int)(no_sync_until - now));
                    return;
                }

                if (cts != null)
                {
                    cts.Cancel();
                    cts = null;
                }
                no_sync_until = now + 5 * 60 * 1000;

                serversync = SyncState.Syncing; // set to syncing!
                BeginSyncEvent?.Invoke(this,EventArgs.Empty);

                Task.Run(async () =>
                {
                    //console.log("dosync: sending GET request");
                    
                    var response = await HttpClient.GetAsync(new Uri(url_base, "Values?after=" + last_sync));

                    if (!response.IsSuccessStatusCode)
                    {
                        lock (locker)
                        {
                            //console.error("dosync: FAILED %s", response.ReasonPhrase);
                            serversync = SyncState.Disabled;
                            onSyncFromServer(false);
                            return;
                        }
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    Dictionary<long, Work> current;
                    Dictionary<long, Work> items;
                    long time;
                    var settings = new JsonSerializerSettings();
                    settings.MissingMemberHandling = MissingMemberHandling.Ignore;

                    lock (locker)
                    {

                        try
                        {
                            items = JsonConvert.DeserializeObject<Dictionary<long, Work>>(content, settings);
                        }
                        catch (Newtonsoft.Json.JsonException /*e*/)
                        {
                            lock (locker)
                            {
                                //console.error("dosync: FAILED %s", e.ToString());
                                serversync = SyncState.Disabled;
                                onSyncFromServer(false);
                                return;
                            }
                        }

                        Dictionary<long, Work> newitems = new Dictionary<long, Work>();
                        foreach (var item in items)
                        {
                            item.Value.id = item.Key;
                            // Highest time value of incoming item is our new sync time
                            if (item.Value.timestamp > last_sync)
                            {
                                last_sync = item.Value.timestamp;
                            }

                            Work old = null;
                            if (!storage.ContainsKey(item.Key) || (old = storage[item.Key]).IsNewerOrSame(item.Value))
                            {
                                // Remove from unsynced list (if it exists)
                                if (unsynced.ContainsKey(item.Key)) { unsynced.Remove(item.Key); }
                                // Grab the new details
                                newitems[item.Key] = storage[item.Key] = item.Value;

                                if (old == null || item.Value.location == null || item.Value.location == 0 || (old != null && item.Value.number > old.number))
                                {
                                    WorkEvents.TryGetEvent(item.Key)?.OnChapterNumChanged(this, item.Value);
                                }
                                else
                                {
                                    WorkEvents.TryGetEvent(item.Key)?.OnLocationChanged(this, item.Value);
                                }
                            }
                            // This kinda shouldn't happen, but apparently it did... we can deal with it though
                            else
                            {
                                // Update the timestamp to newer than newest
                                if (storage[item.Key].timestamp <= item.Value.timestamp) { storage[item.Key].timestamp = item.Value.timestamp + 1; }
                                else { item.Value.timestamp += 1; }
                                // set as unsynced
                                unsynced[item.Key] = storage[item.Key];
                            }
                        }
                        App.Database.SaveItems(newitems.Values);

                        current = unsynced;
                        unsynced = new Dictionary<long, Work>();
                        time = last_sync;

                        if (current.Count == 0)
                        {
                            App.Database.SaveVariable("last_sync", last_sync.ToString());
                            serversync = SyncState.Ready;
                            onSyncFromServer(true);
                            return;
                        }
                    }

                    foreach (var item in current.Values)
                    {
                        if (item.timestamp > time)
                        {
                            time = item.timestamp;
                        }
                    }

                    var json = JsonConvert.SerializeObject(current);
                    var postBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    response = await HttpClient.PostAsync(new Uri(url_base, "Values"), postBody);
                    if (!response.IsSuccessStatusCode)
                    {
                        lock (locker)
                        {
                            //console.error("dosync: FAILED %s", response.ReasonPhrase);
                            serversync = SyncState.Disabled;
                            App.Database.SaveVariable("last_sync", 0L.ToString());
                            last_sync = 0;
                            onSyncFromServer(false);
                            return;
                        }
                    }

                    content = await response.Content.ReadAsStringAsync();

                    try
                    {
                        items = JsonConvert.DeserializeObject<Dictionary<long, Work>>(content, settings);
                    }
                    catch (Newtonsoft.Json.JsonException /*e*/)
                    {
                        lock (locker)
                        {
                            //console.error("dosync: FAILED %s", e.ToString());
                            serversync = SyncState.Disabled;
                            App.Database.SaveVariable("last_sync", 0L.ToString());
                            last_sync = 0;
                            onSyncFromServer(false);
                            return;
                        }
                    }

                    //console.log("dosync: SUCCESS. %i conflicted items", Object.keys(items).length);
                    lock (locker)
                    {
                        if (items.Count > 0)
                        {
                            App.Database.SaveVariable("last_sync", 0L.ToString());
                            last_sync = 0;
                            dosync(true);
                            return;
                        }
                        if (time > last_sync)
                        {
                            last_sync = time;
                            App.Database.SaveVariable("last_sync", time.ToString());
                        }

                        if (unsynced.Count > 0)
                        {
                            dosync(true);
                        }
                        else
                        {
                            serversync = SyncState.Ready;
                            onSyncFromServer(true);
                        }
                    }

                });
            }
        }

        public void dosync(Action<bool> callback)
        {
            lock (locker)
            {
                if (serversync == SyncState.Disabled)
                {
                    SyncFromServerEvent += (sender, success) => callback(success);
                    onSyncFromServer(false);
                }
                else
                {
                    SyncFromServerEvent += (sender, success) => callback(success);
                    if (serversync != SyncState.Syncing) { dosync(true); }
                }
            }
        }

        public Task<IDictionary<long, WorkChapter>> getWorkChaptersAsync(IEnumerable<long> works)
        {
            return Task.Run(() =>
            {
                ManualResetEventSlim handle = null;
                IDictionary<long, WorkChapter> r = new Dictionary<long, WorkChapter>();

                EventHandler<bool> sendResponse = (sender, success) =>
                {
                    foreach (long w in works)
                    {
                        Work work;
                        if (storage.TryGetValue(w, out work))
                        {
                            r[w] = new WorkChapter(work);
                        }
                    }
                    if (handle != null) handle.Set();
                };
                lock (locker)
                {
                    if (serversync == SyncState.Syncing)
                    {
                        handle = new ManualResetEventSlim();
                        SyncFromServerEvent += sendResponse;
                    }
                    else
                    {
                        sendResponse(this, true);
                    }
                }
                if (handle != null) handle.Wait();
                return r;

            });
        }

        public void setWorkChapters(IDictionary<long, WorkChapter> works)
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
                            var w = new Work
                            {
                                id = work.Key,
                                number = work.Value.number,
                                chapterid = work.Value.chapterid,
                                location = work.Value.location,
                                timestamp = time
                            };

                            unsynced[work.Key] = newitems[work.Key] = storage[work.Key] = w;

                            // Do a delayed since if we finished a chapter, or started a new one 
                            if (old == null || work.Value.location == null || work.Value.location == 0 || (old != null && work.Value.number > old.number))
                            {
                                do_delayed = true;
                                WorkEvents.TryGetEvent(work.Key)?.OnChapterNumChanged(this, w);
                            }
                            else
                            {
                                WorkEvents.TryGetEvent(work.Key)?.OnLocationChanged(this, w);
                            }
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

        public Task<Dictionary<string, string>> Login(string username, string password)
        {
            return Task.Run(async () =>
            {
                Dictionary<string, string> errors = null;

                lock (locker)
                {
                    serversync = SyncState.Disabled;
                    HttpClient.DefaultRequestHeaders.Authorization = null;
                    App.Database.SaveVariable("authorization.username", "");
                    App.Database.SaveVariable("authorization.credential", "");
                }

                var json = JsonConvert.SerializeObject(new
                {
                    username = username,
                    password = password
                });

                var postBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync(new Uri(url_base, "User/Login"), postBody);
                if (!response.IsSuccessStatusCode)
                {
                    errors = new Dictionary<string, string> {
                        {  "server", response.ReasonPhrase }
                    };
                    return errors;
                }

                var content = await response.Content.ReadAsStringAsync();

                try
                {
                    errors = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                    return errors;
                }
                catch (Newtonsoft.Json.JsonException)
                {
                }

                try
                {
                    string cred = JsonConvert.DeserializeObject<string>(content);
                    lock (locker)
                    {
                        Authorization authorization;
                        authorization.credential = cred;
                        authorization.username = username;
                        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Ao3track", authorization.toBase64());
                        last_sync = 0;
                        App.Database.SaveVariable("authorization.username", authorization.username);
                        App.Database.SaveVariable("authorization.credential", authorization.credential);
                        App.Database.SaveVariable("last_sync", last_sync.ToString());
                        serversync = SyncState.Syncing;
                        dosync(true);
                    }
                    errors = new Dictionary<string, string>();
                }
                catch (Newtonsoft.Json.JsonException)
                {
                }

                return errors;
            });
        }

        public Task<Models.ServerReadingList> SyncReadingListAsync(Models.ServerReadingList srl)
        {
            return Task.Run(async () =>
            {
                if (serversync == SyncState.Disabled)
                {
                    return null;
                }

                var json = JsonConvert.SerializeObject(srl);
                var postBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync(new Uri(url_base, "ReadingList"), postBody);
                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                Models.ServerReadingList res = null;

                try
                {
                    res = JsonConvert.DeserializeObject<Models.ServerReadingList>(content);
                }
                catch (Newtonsoft.Json.JsonException /*e*/)
                {
                }
                
                return res;
            });
        }
    }
}