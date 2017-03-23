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
            public string ToBase64()
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(this.username + '\n' + this.credential));
            }
        };

        public event EventHandler BeginSyncEvent;
        public event EventHandler<EventArgs<bool>> EndSyncEvent;

        public bool IsSyncing
        {
            get { return serversync == SyncState.Syncing; }
        }

        public bool CanSync
        {
            get { return serversync != SyncState.Disabled; }
        }

        public string Username
        {
            get { return App.Database.GetVariable("authorization.username") ?? ""; }
        }


        event EventHandler<bool> SyncFromServerEvent;

        HttpClient HttpClient { get; set; }

        public SyncedStorage()
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                //httpClientHandler.MaxConnectionsPerServer = 2;
                MaxRequestContentBufferSize = 1 << 20
            };
            HttpClient = new HttpClient(httpClientHandler);

            storage = new Dictionary<long, Work>();
            unsynced = new Dictionary<long, Work>();

            var database = App.Database;

            database.TryGetVariable("last_sync", long.TryParse, out last_sync);

            Authorization authorization;
            authorization.username = database.GetVariable("authorization.username") ?? "";
            authorization.credential = database.GetVariable("authorization.credential") ?? "";
            if (String.IsNullOrEmpty(authorization.username) || String.IsNullOrEmpty(authorization.credential))
            {
                serversync = SyncState.Disabled;
            }
            else
            {
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Ao3track", authorization.ToBase64());
                serversync = SyncState.Syncing;
            }

            foreach (var it in database.GetItems())
            {
                storage[it.workid] = it;

                if (storage[it.workid].Timestamp > last_sync)
                {
                    unsynced[it.workid] = storage[it.workid];
                }
            }
            DoSync();
        }

        void OonSyncFromServer(bool success)
        {
            lock (locker)
            {
                EndSyncEvent?.Invoke(this, new EventArgs<bool>(success));
                var e = SyncFromServerEvent;
                SyncFromServerEvent = null;
                Task.Run(() =>
                {
                    e?.Invoke(this, success);
                });
            }
        }

        public void DelayedSync(int timeout)
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
                        DoSync(true);
                    }
                });
            }

        }


        public void DoSync(bool force = false)
        {
            lock (locker)
            {
                if (serversync == SyncState.Disabled)
                {
                    OonSyncFromServer(false);
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
                    DelayedSync((int)(no_sync_until - now));
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

                    var task = HttpClient.GetAsync(new Uri(url_base, "Values?after=" + last_sync));
                    try
                    {
                        task.Wait();
                    }
                    catch (Exception)
                    {

                    }
                    var response = task.IsFaulted?null:task.Result;

                    if (response == null || !response.IsSuccessStatusCode)
                    {
                        lock (locker)
                        {
                            //console.error("dosync: FAILED %s", response.ReasonPhrase);
                            if (response != null && response.StatusCode == HttpStatusCode.Forbidden)
                                serversync = SyncState.Disabled;
                            else
                                serversync = SyncState.Ready;
                            OonSyncFromServer(false);
                            return;
                        }
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    Dictionary<long, Work> current;
                    Dictionary<long, Work> items;
                    long time, old_sync;
                    var settings = new JsonSerializerSettings()
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };
                    lock (locker)
                    {
                        old_sync = last_sync;

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
                                OonSyncFromServer(false);
                                return;
                            }
                        }

                        Dictionary<long, Work> newitems = new Dictionary<long, Work>();
                        foreach (var item in items)
                        {
                            item.Value.workid = item.Key;
                            // Highest time value of incoming item is our new sync time
                            if (item.Value.Timestamp > last_sync)
                            {
                                last_sync = item.Value.Timestamp;
                            }

                            Work old = null;
                            if (!storage.ContainsKey(item.Key) || (old = storage[item.Key]).LessThanOrEqual(item.Value))
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
                                if (storage[item.Key].Timestamp <= item.Value.Timestamp) { storage[item.Key].Timestamp = item.Value.Timestamp + 1; }
                                else { item.Value.Timestamp += 1; }
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
                            App.Database.SaveVariable("last_sync", last_sync);
                            serversync = SyncState.Ready;
                            OonSyncFromServer(true);
                            return;
                        }
                    }

                    foreach (var item in current.Values)
                    {
                        if (item.Timestamp > time)
                        {
                            time = item.Timestamp;
                        }
                    }

                    var json = JsonConvert.SerializeObject(current);
                    var postBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    task = HttpClient.PostAsync(new Uri(url_base, "Values"), postBody);
                    try
                    {
                        task.Wait();
                    }
                    catch (Exception)
                    {

                    }
                    response = task.IsFaulted ? null : task.Result;
                    if (response == null || !response.IsSuccessStatusCode)
                    {
                        lock (locker)
                        {
                            //console.error("dosync: FAILED %s", response.ReasonPhrase);
                            if (response != null && response.StatusCode == HttpStatusCode.Forbidden)
                            {
                                last_sync = 0;
                                serversync = SyncState.Disabled;
                            }
                            else
                            {
                                last_sync = old_sync;
                                serversync = SyncState.Ready;
                                unsynced = current;
                            }
                            App.Database.SaveVariable("last_sync", last_sync);
                            OonSyncFromServer(false);
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
                            App.Database.SaveVariable("last_sync", 0L);
                            last_sync = 0;
                            OonSyncFromServer(false);
                            return;
                        }
                    }

                    //console.log("dosync: SUCCESS. %i conflicted items", Object.keys(items).length);
                    lock (locker)
                    {
                        if (items.Count > 0)
                        {
                            App.Database.SaveVariable("last_sync", 0L);
                            last_sync = 0;
                            DoSync(true);
                            return;
                        }
                        if (time > last_sync)
                        {
                            last_sync = time;
                            App.Database.SaveVariable("last_sync", time);
                        }

                        if (unsynced.Count > 0)
                        {
                            DoSync(true);
                        }
                        else
                        {
                            serversync = SyncState.Ready;
                            OonSyncFromServer(true);
                        }
                    }

                });
            }
        }

        public void DoSync(Action<bool> callback)
        {
            lock (locker)
            {
                if (serversync == SyncState.Disabled)
                {
                    SyncFromServerEvent += (sender, success) => callback(success);
                    OonSyncFromServer(false);
                }
                else
                {
                    SyncFromServerEvent += (sender, success) => callback(success);
                    if (serversync != SyncState.Syncing) { DoSync(true); }
                }
            }
        }

        public async Task<IDictionary<long, WorkChapter>> GetWorkChaptersAsync(IEnumerable<long> works)
        {
            return await Task.Run(() =>
            {
                ManualResetEventSlim handle = null;
                IDictionary<long, WorkChapter> r = new Dictionary<long, WorkChapter>();

                EventHandler<bool> sendResponse = (sender, success) =>
                {
                    foreach (long w in works)
                    {
                        if (storage.TryGetValue(w, out Work work))
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

        public void SetWorkChapters(IDictionary<long, WorkChapter> works)
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
                        if (work.Value.workid == 0) work.Value.workid = work.Key;
                        else if (work.Value.workid != work.Key) throw new ArgumentException("Value.workid != Key", nameof(works));

                        if (!storage.TryGetValue(work.Key, out var old) || old.LessThan(work.Value))
                        {
                            var seq  = work.Value.seq;
                            if (seq == null && old != null) { 
                                seq = old.Seq;
                            }

                            var w = new Work
                            {
                                workid = work.Key,
                                number = work.Value.number,
                                chapterid = work.Value.chapterid,
                                location = work.Value.location,
                                Timestamp = time,
                                Seq = seq ?? 0,
                            };

                            unsynced[work.Key] = newitems[work.Key] = storage[work.Key] = w;

                            // Do a delayed since if we finished a chapter, or started a new one 
                            if (old == null || work.Value.location == null || work.Value.location == 0 || work.Value.number > old.number || work.Value.seq > old.Seq)
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
                                DelayedSync(10 * 1000);
                            }
                            else
                            {
                                DoSync();
                            }
                        }
                    }
                }
            });
        }

        public Task<Dictionary<string, string>> UserCreate(string username, string password, string email)
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
                    password = password,
                    email = email
                });

                var postBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var task = HttpClient.PostAsync(new Uri(url_base, "User/Create"), postBody);
                task.Wait();
                if (task.IsFaulted) return new Dictionary<string, string>
                {
                    {  "exception", task.Exception.Message }
                };
                var response = task.Result;
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
                        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Ao3track", authorization.ToBase64());
                        last_sync = 0;
                        App.Database.SaveVariable("authorization.username", authorization.username);
                        App.Database.SaveVariable("authorization.credential", authorization.credential);
                        App.Database.SaveVariable("last_sync", last_sync);
                        serversync = SyncState.Syncing;
                        DoSync(true);
                    }
                    errors = new Dictionary<string, string>();
                }
                catch (Newtonsoft.Json.JsonException)
                {
                }

                return errors;
            });
        }
        public async Task UserLogout()
        {
            await Task.Run(() =>
                {
                    lock (locker)
                    {
                        serversync = SyncState.Disabled;
                        HttpClient.DefaultRequestHeaders.Authorization = null;
                        last_sync = 0;
                        App.Database.SaveVariable("authorization.username", "");
                        App.Database.SaveVariable("authorization.credential", "");
                        App.Database.SaveVariable("last_sync", last_sync);
                    }
                }
            );
        }

        public Task<Dictionary<string, string>> UserLogin(string username, string password)
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

                var task = HttpClient.PostAsync(new Uri(url_base, "User/Login"), postBody);
                task.Wait();
                if (task.IsFaulted) return new Dictionary<string, string>
                {
                    {  "exception", task.Exception.Message }
                };
                var response = task.Result;
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
                        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Ao3track", authorization.ToBase64());
                        last_sync = 0;
                        App.Database.SaveVariable("authorization.username", authorization.username);
                        App.Database.SaveVariable("authorization.credential", authorization.credential);
                        App.Database.SaveVariable("last_sync", last_sync);
                        serversync = SyncState.Syncing;
                        DoSync(true);
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

                var task = HttpClient.PostAsync(new Uri(url_base, "ReadingList"), postBody);
                try
                {
                    task.Wait();
                }
                catch (Exception)
                {

                }
                if (task.IsFaulted) return null;
                var response = task.Result;
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

        public Task<bool> SubmitErrorReport(string report)
        {
            return Task.Run(() =>
            {
                var json = JsonConvert.SerializeObject(report);
                var postBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var task = HttpClient.PostAsync(new Uri(url_base, "ErrorReport"), postBody);
                try
                {
                    task.Wait();
                }
                catch (Exception)
                {

                }
                if (task.IsFaulted) return false;
                var response = task.Result;
                if (!response.IsSuccessStatusCode)
                    return false;
                return true;
            });
        }
    }
}