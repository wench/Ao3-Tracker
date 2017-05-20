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
        ReaderWriterLockSlim ReadWriteLock { get; } = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

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

        public bool HaveCredentials
        {
            get { return !string.IsNullOrWhiteSpace(App.Database.GetVariable("authorization.username")) && !string.IsNullOrWhiteSpace(App.Database.GetVariable("authorization.credential")); }
        }


        private HttpClient HttpClient { get; set; }

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

            // Time synchronization. 
            var startTime = DateTime.UtcNow.ToUnixTime();
            HttpClient.GetAsync(new Uri(url_base, "Values/Time")).ContinueWith(async (task) =>
            {
                if (task.IsCanceled || task.IsFaulted) return;
                var endTime = DateTime.UtcNow.ToUnixTime();

                var response = task.Result;
                var content = await response.Content.ReadAsStringAsync();

                var serverTime = JsonConvert.DeserializeObject<long>(content);

                Extensions.UnixTimeOffset = serverTime - (startTime + endTime) / 2;
            });

            storage = new Dictionary<long, Work>();
            unsynced = new Dictionary<long, Work>();

            var database = App.Database;

            database.TryGetVariable("last_sync", long.TryParse, out last_sync);

            Authorization authorization;
            authorization.username = database.GetVariable("authorization.username") ?? "";
            authorization.credential = database.GetVariable("authorization.credential") ?? "";
            if (String.IsNullOrWhiteSpace(authorization.username) || String.IsNullOrWhiteSpace(authorization.credential))
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
        }

        private CancellationTokenSource DelayedSyncCTS { get; set; } = null;
        public Task DelayedSyncAsync(int timeout)
        {
            return Task.Run(() =>
            {
                using (ReadWriteLock.WriteLock())
                {
                    //console.log("delayedsync: timeout = %i", timeout);
                    var now = DateTime.UtcNow.Ticks / 10000;
                    if (DelayedSyncCTS != null)
                    {
                        //console.log("delayedsync: existing pending sync in %i", no_sync_until - now);
                        // If the pending sync is going to happen before timeout would elapse, just let it happen
                        if (no_sync_until <= now + timeout) { return; }
                        DelayedSyncCTS.Cancel();
                        DelayedSyncCTS = null;
                    }
                    //console.log("delayedsync: setting up timeout callback");
                    no_sync_until = now + timeout;
                    serversync = SyncState.Delayed;
                    var cts = DelayedSyncCTS = new CancellationTokenSource();

                    Task.Delay(timeout, DelayedSyncCTS.Token).ContinueWith(task =>
                    {
                        using (ReadWriteLock.WriteLock())
                        {
                            cts.Dispose();
                            if (cts == DelayedSyncCTS) DelayedSyncCTS = null;

                            if (task.IsCompleted && serversync != SyncState.Syncing) DoSyncAsync(true);
                        }
                    });
                }
            });
        }

        public void DoSyncAsync(bool force = false)
        {
            Task.Run(() =>
            {
                using (ReadWriteLock.UpgradeableReadLock())
                {
                    var settings = new JsonSerializerSettings()
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };

                    Dictionary<long, Work> items;
                    Dictionary<long, Work> current;
                    long time;
                    long old_sync;

                    Task<HttpResponseMessage> responseTask;
                    HttpResponseMessage response;
                    Task<string> contentTask;
                    string content;

                    using (ReadWriteLock.WriteLock())
                    {
                        if (serversync == SyncState.Disabled)
                        {
                            EndSyncEvent?.Invoke(this, new EventArgs<bool>(false));
                            //console.warn("dosync: FAILED. No credentials");
                            return;
                        }

                        // if we have waiting readers do it now
                        if (ReadWriteLock.WaitingReadCount > 0)
                        {
                            force = true;
                        }

                        // Enforce 5 minutes gap between server sync. Don't want to hammer the server while scrolling through a fic  
                        var now = DateTime.UtcNow.Ticks / 10000;
                        if (!force && now < no_sync_until)
                        {
                            //console.log("dosync: have to wait %i for timeout", no_sync_until - now);
                            DelayedSyncAsync((int)(no_sync_until - now));
                            return;
                        }

                        serversync = SyncState.Syncing; // set to syncing!

                        if (DelayedSyncCTS != null)
                        {
                            DelayedSyncCTS.Cancel();
                            DelayedSyncCTS = null;
                        }
                        no_sync_until = now + 5 * 60 * 1000;                    

                        BeginSyncEvent?.Invoke(this, EventArgs.Empty);

                        //console.log("dosync: sending GET request");

                        responseTask = HttpClient.GetAsync(new Uri(url_base, "Values?after=" + last_sync));
                        responseTask.TryWait();
                        response = responseTask.IsFaulted ? null : responseTask.Result;

                        if (response == null || !response.IsSuccessStatusCode)
                        {
                            using (ReadWriteLock.WriteLock())
                            {
                                //console.error("dosync: FAILED %s", response.ReasonPhrase);
                                if (response != null && response.StatusCode == HttpStatusCode.Forbidden)
                                    serversync = SyncState.Disabled;
                                else
                                    serversync = SyncState.Ready;
                                EndSyncEvent?.Invoke(this, new EventArgs<bool>(false));
                                return;
                            }
                        }

                        contentTask = response.Content.ReadAsStringAsync();
                        contentTask.Wait();
                        content = contentTask.Result;

                        old_sync = last_sync;

                        try
                        {
                            items = JsonConvert.DeserializeObject<Dictionary<long, Work>>(content, settings);
                        }
                        catch (Exception e)
                        {
                            App.Log(e);
                            serversync = SyncState.Disabled;
                            EndSyncEvent?.Invoke(this, new EventArgs<bool>(false));
                            return;
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
                        App.Database.SaveItems(newitems.Values.ToReadOnly());

                        current = unsynced;
                        unsynced = new Dictionary<long, Work>();
                        time = last_sync;

                        if (current.Count == 0)
                        {
                            App.Database.SaveVariable("last_sync", last_sync);
                            serversync = SyncState.Ready;
                            EndSyncEvent?.Invoke(this, new EventArgs<bool>(true));
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
                    responseTask = HttpClient.PostAsync(new Uri(url_base, "Values"), postBody);
                    responseTask.TryWait();
                    response = responseTask.IsFaulted ? null : responseTask.Result;

                    if (response == null || !response.IsSuccessStatusCode)
                    {
                        using (ReadWriteLock.WriteLock())
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
                            EndSyncEvent?.Invoke(this, new EventArgs<bool>(false));
                            return;
                        }
                    }

                    contentTask = response.Content.ReadAsStringAsync();
                    contentTask.Wait();
                    content = contentTask.Result;

                    try
                    {
                        items = JsonConvert.DeserializeObject<Dictionary<long, Work>>(content, settings);
                    }
                    catch (Exception e)
                    {
                        App.Log(e);
                        using (ReadWriteLock.WriteLock())
                        {
                            serversync = SyncState.Disabled;
                            App.Database.SaveVariable("last_sync", 0L);
                            last_sync = 0;
                            EndSyncEvent?.Invoke(this, new EventArgs<bool>(false));
                            return;
                        }
                    }

                    //console.log("dosync: SUCCESS. %i conflicted items", Object.keys(items).length);
                    using (ReadWriteLock.WriteLock())
                    {
                        if (items.Count > 0)
                        {
                            App.Database.SaveVariable("last_sync", 0L);
                            last_sync = 0;
                            DoSyncAsync(true);
                            return;
                        }
                        if (time > last_sync)
                        {
                            last_sync = time;
                            App.Database.SaveVariable("last_sync", time);
                        }

                        if (unsynced.Count > 0)
                        {
                            DoSyncAsync(true);
                        }
                        else
                        {
                            serversync = SyncState.Ready;
                            EndSyncEvent?.Invoke(this, new EventArgs<bool>(true));
                        }
                    }
                }
            });
        }

        public Task<IDictionary<long, WorkChapter>> GetWorkChaptersAsync(IEnumerable<long> works)
        {
            return Task.Run(() =>
            {
                using (ReadWriteLock.ReadLock())
                {
                    IDictionary<long, WorkChapter> r = new Dictionary<long, WorkChapter>();

                    foreach (long w in works)
                    {
                        if (storage.TryGetValue(w, out Work work))
                        {
                            r[w] = new WorkChapter(work);
                        }
                    }

                    return r;
                }
            });
        }

        public Task SetWorkChaptersAsync(IDictionary<long, WorkChapter> works)
        {
            return Task.Run(() =>
            {
                using (ReadWriteLock.WriteLock())
                {
                    long time = DateTime.UtcNow.ToUnixTime();
                    Dictionary<long, Work> newitems = new Dictionary<long, Work>();
                    bool do_delayed = false;

                    foreach (var work in works)
                    {
                        if (work.Value.workid == 0) work.Value.workid = work.Key;
                        else if (work.Value.workid != work.Key) throw new ArgumentException("Value.workid != Key", nameof(works));

                        if (!storage.TryGetValue(work.Key, out var old) || old.LessThan(work.Value))
                        {
                            var seq = work.Value.seq;
                            if (seq == null && old != null)
                            {
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
                        App.Database.SaveItems(newitems.Values.ToReadOnly());

                        if (serversync == SyncState.Ready || serversync == SyncState.Delayed)
                        {
                            if (do_delayed)
                            {
                                DelayedSyncAsync(10 * 1000);
                            }
                            else
                            {
                                DoSyncAsync();
                            }
                        }
                    }
                }
            });
        }

        public Task UserLogoutAsync()
        {
            return Task.Run(() =>
                {
                    using (ReadWriteLock.WriteLock())
                    {
                        serversync = SyncState.Disabled;
                        HttpClient.DefaultRequestHeaders.Authorization = null;
                        last_sync = 0;
                        App.Database.SaveVariable("authorization.credential", "");
                        App.Database.SaveVariable("last_sync", last_sync);
                    }
                }
            );
        }

        public Task<Dictionary<string, string>> UserLoginAsync(string username, string password)
        {
            return UserLoginOrCreateAsync(username, password, null);
        }

        public Task<Dictionary<string, string>> UserCreateAsync(string username, string password, string email)
        {
            return UserLoginOrCreateAsync(username, password, email ?? "");
        }

        private Task<Dictionary<string, string>> UserLoginOrCreateAsync(string username, string password, string email)
        {
            return Task.Run(() =>
            {
                using (ReadWriteLock.WriteLock())
                {
                    serversync = SyncState.Disabled;
                    HttpClient.DefaultRequestHeaders.Authorization = null;
                    App.Database.SaveVariable("authorization.credential", "");

                    Task<HttpResponseMessage> responseTask;

                    if (email == null)
                    {

                        var json = JsonConvert.SerializeObject(new
                        {
                            username = username,
                            password = password
                        });

                        var postBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        responseTask = HttpClient.PostAsync(new Uri(url_base, "User/Login"), postBody);
                    }
                    else
                    {
                        var json = JsonConvert.SerializeObject(new
                        {
                            username = username,
                            password = password,
                            email = email
                        });

                        var postBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        responseTask = HttpClient.PostAsync(new Uri(url_base, "User/Create"), postBody);
                    }

                    responseTask.TryWait();
                    if (responseTask.IsFaulted)
                    {
                        return new Dictionary<string, string> {
                        {  "exception", responseTask.Exception.Message }
                    };
                    }
                    var response = responseTask.Result;
                    if (!response.IsSuccessStatusCode)
                    {
                        return new Dictionary<string, string> {
                        {  "server", response.ReasonPhrase }
                    };
                    }

                    var contentTask = response.Content.ReadAsStringAsync();
                    contentTask.Wait();
                    var content = contentTask.Result;

                    try
                    {
                        if (content.StartsWith("{"))
                        {
                            return JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                        }

                        string cred = JsonConvert.DeserializeObject<string>(content);

                        Authorization authorization;
                        authorization.credential = cred;
                        authorization.username = username;
                        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Ao3track", authorization.ToBase64());
                        last_sync = 0;
                        App.Database.SaveVariable("authorization.username", authorization.username);
                        App.Database.SaveVariable("authorization.credential", authorization.credential);
                        App.Database.SaveVariable("last_sync", last_sync);
                        serversync = SyncState.Syncing;
                        DoSyncAsync(true);
                    }
                    catch (Exception e)
                    {
                        App.Log(e);
                        return new Dictionary<string, string> {
                            {  "exception", e.Message }
                        };
                    }

                    return null;
                }
            });
        }

        public Task<Models.ServerReadingList> SyncReadingListAsync(Models.ServerReadingList srl)
        {
            return Task.Run(async () =>
            {
                if (serversync == SyncState.Disabled) return null;

                var json = JsonConvert.SerializeObject(srl);
                var postBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var responseTask = HttpClient.PostAsync(new Uri(url_base, "ReadingList"), postBody);
                responseTask.TryWait();
                if (responseTask.IsFaulted) return null;

                var response = responseTask.Result;
                if (!response.IsSuccessStatusCode) return null;

                var content = await response.Content.ReadAsStringAsync();

                try
                {
                    return JsonConvert.DeserializeObject<Models.ServerReadingList>(content);
                }
                catch 
                {
                    return null;
                }
            });
        }

        public Task<bool> SubmitErrorReport(string report)
        {
            return Task.Run(() =>
            {
                var json = JsonConvert.SerializeObject(report);
                var postBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var responseTask = HttpClient.PostAsync(new Uri(url_base, "ErrorReport"), postBody);
                responseTask.TryWait();
                if (responseTask.IsFaulted) return false;
                return responseTask.Result.IsSuccessStatusCode;
            });
        }
    }
}