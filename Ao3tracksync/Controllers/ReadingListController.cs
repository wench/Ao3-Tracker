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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Ao3tracksync.Models;
using System.Web.UI;

namespace Ao3tracksync.Controllers
{
    [Authorize(Roles = "users"), AllowCrossSite, System.Web.Mvc.OutputCache(Location = OutputCacheLocation.None)]
    public class ReadingListController : ApiController
    {
        public new User User { get { return (base.User as Auth.UserPrincipal)?.User; } }

        public class ReadingList
        {
            public long last_sync { get; set; }
            public Dictionary<string, long> paths { get; set; }
        }

        #region OPTIONS api/ReadingList
        [AllowAnonymous, CrossSiteOptions]
        public void Options()
        {
        }
        #endregion

        // POST: api/ReadingList
        public ReadingList Post([FromBody]ReadingList incoming)
        {
            using (var ctx = new Models.Ao3TrackEntities())
            {
                var changes = new Dictionary<string, long>();

                var existing = (from i in ctx.ReadingLists where i.userid == User.id select i).ToDictionary(i => i.path);
                Models.ReadingList ls;
                if (existing.TryGetValue("", out ls)) {
                    existing.Remove("");
                }
                else
                {
                    ls = ctx.ReadingLists.Add(new Models.ReadingList { userid = User.id, path = "", timestamp = 0 });
                }
                var latest = ls.timestamp;

                foreach (var item in existing)
                {
                    if (item.Value.timestamp > latest) latest = item.Value.timestamp;

                    if (!incoming.paths.ContainsKey(item.Key))
                    {
                        if (item.Value.timestamp >= incoming.last_sync)
                        {
                            // Item was added and needs to be sent to client
                            changes.Add(item.Key, item.Value.timestamp);
                        }
                        else
                        {
                            // Client deleted item since we last sent it
                            ctx.ReadingLists.Remove(item.Value);
                        }
                    }
                    else
                    {
                        incoming.paths.Remove(item.Key);
                    }                    
                }

                foreach (var item in incoming.paths)
                {
                    if (item.Value > latest) latest = item.Value;

                    if (!existing.ContainsKey(item.Key))
                    {
                        if (item.Value >= ls.timestamp)
                        {
                            // Item is NEW!
                            ctx.ReadingLists.Add(new Models.ReadingList
                            {
                                userid = User.id, path = item.Key, timestamp = item.Value
                            });
                        }
                        else
                        {
                            // Item was previously deleted and client needs to be notified
                            changes.Add(item.Key, -1);
                        }
                    }
                }

                ls.timestamp = latest+1;

                ctx.SaveChanges();

                return new ReadingList { last_sync = ls.timestamp, paths = changes };
            }

        }
    }
}
