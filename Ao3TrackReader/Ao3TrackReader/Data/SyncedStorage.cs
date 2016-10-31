using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;

namespace Ao3TrackReader.Data
{
    class SyncedStorage
    {
        static object locker = new object();

        HttpClient client;

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
            client = new HttpClient();
            storage = new Dictionary<long, Work>();
            unsynced = new Dictionary<long, Work>();

            readFromDatabase();
        }

        void onSyncFromServer(bool success)
        {
            var e = SyncFromServerEvent;
            SyncFromServerEvent = null;
            Task.Run(() =>
            {
                e?.Invoke(this, success);
            });
        }

        void delayedsync(int timeout)
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

        void readFromDatabase()
        {
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

            window.setInterval(dosync, 1000 * 60 * 60 * 6);

        }
    }
}
