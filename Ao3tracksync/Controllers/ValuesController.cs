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
using System.Web.Hosting;
//using System.Web.Mvc;
using System.Web.UI;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using Ao3tracksync.Models;
using System.Collections;
using System.Web.Http.Filters;
using System.Configuration;
using System.Threading;
using System.Web;

namespace Ao3tracksync.Controllers
{
    [RoutePrefix("api/Values"), Authorize(Roles = "users"), AllowCrossSite, System.Web.Mvc.OutputCache(Location = OutputCacheLocation.None)]
    public class ValuesController : ApiController
    {
        public static bool Initialized { get; private set; }
        static ValuesController() { Initialized = true; }

        public new User User { get { return (base.User as Auth.UserPrincipal)?.User; } }

        /// <summary>
        /// Current chapter and location information for a work
        /// </summary>
        /// <remarks>
        /// When comparing chapters current location is considered to be the workchapter with:
        /// 1) highest seq
        /// 2) highest chapter number
        /// 3) highest location value
        /// </remarks>
        public class WorkChapter
        {
            /// <summary>
            /// Defaut constructor
            /// </summary>
            public WorkChapter()
            {
            }
            /// <summary>
            /// Construct from a Database Model Work
            /// </summary>
            /// <param name="work">Work from database</param>
            public WorkChapter(Work work)
            {
                if (work != null)
                {
                    chapterid = work.chapterid;
                    number = work.number;
                    location = work.location;
                    timestamp = work.timestamp;
                    seq = work.seq;
                }
            }
            /// <summary>Id of current chapter</summary>
            public long chapterid { get; set; }
            /// <summary>Oridinal number of current chapter</summary>
            public long number { get; set; }
            /// <summary>
            /// Current location in chapter
            /// </summary>
            /// <remarks>
            /// Start of chapter: <c>location = 0;</c>
            /// End of chapter: <c>location = null;</c>
            /// Start of paragraph: <c>location = paragraph * 1000000000;</c>
            /// Part way through paragraph: <c>location = paragraph * 1000000000 + frac * 479001600;</c>
            /// End of paragraph: <c>location = paragraph * 1000000000 + 500000000;</c>
            /// </remarks>
            public long? location { get; set; }
            /// <summary>
            /// Timestamp when chapter or location last updated.
            /// </summary>
            public long timestamp { get; set; }
            /// <summary>
            /// Sequence number
            /// </summary>
            /// <remarks>
            /// WorkChapters with higher sequences always replace ones with lower 
            /// </remarks>
            public long seq { get; set; }
        };

        #region GET api/User/Init
        [AllowAnonymous, HttpGet, Route("Init")]
        public void Init()
        {
            using (var ctx = new Models.Ao3TrackEntities())
            {
            }
        }
        #endregion

        #region OPTIONS api/values
        [AllowAnonymous, CrossSiteOptions]
        public void Options()
        {
        }
        #endregion

        /// <summary>
        /// GET api/values
        /// Get all of logged in user's work chapters
        /// </summary>
        /// <returns>[workid]: WorkChapter</returns>
        public IDictionary<long, WorkChapter> Get()
        {
            Int64 timestamp = 0;
            try
            {
                timestamp = Convert.ToInt64(Request.RequestUri.ParseQueryString().Get("after"));
            }
            catch
            {
            }

            using (var ctx = new Models.Ao3TrackEntities())
            {
                ctx.Configuration.AutoDetectChangesEnabled = false;
                return (from works in ctx.Works
                        where works.userid == User.id && works.timestamp > timestamp
                        select works).AsEnumerable().ToDictionary(works => works.id, works => new WorkChapter(works));
            }
        }

        /// <summary>
        /// GET api/values/5
        /// Get single Work Chapter
        /// </summary>
        /// <param name="id">Workid</param>
        /// <returns>[workid]: WorkChapter</returns>
        public IDictionary<long, WorkChapter> Get(long id)
        {
            Int64 timestamp = 0;
            try
            {
                timestamp = Convert.ToInt64(Request.RequestUri.ParseQueryString().Get("after"));
            }
            catch
            {
            }

            using (var ctx = new Models.Ao3TrackEntities())
            {
                ctx.Configuration.AutoDetectChangesEnabled = false;
                var work = (from works in ctx.Works
                            where works.userid == User.id && works.id == id && works.timestamp > timestamp
                            select works).AsEnumerable().FirstOrDefault();

                var res = new Dictionary<long, WorkChapter> { };
                if (work != null) res.Add(id, new WorkChapter(work));
                return res;
            }
        }


        /// <summary>
        /// POST api/values
        /// </summary>
        /// <param name="values">WorkChapterse to add and/or update</param>
        /// <returns>Existing items if there are conflicts</returns>
        public IDictionary<long, WorkChapter> Post([FromBody]IDictionary<long, WorkChapter> values)
        {
            Dictionary<long, WorkChapter> conflicts = new Dictionary<long, WorkChapter>();
            using (var ctx = new Models.Ao3TrackEntities())
            {
                ctx.Configuration.AutoDetectChangesEnabled = true;
                var table = ctx.Works;
                foreach (var v in values)
                {
                    var item = (from works in table
                                where works.userid == User.id && works.id == v.Key
                                select works).AsEnumerable().FirstOrDefault();

                    if (item != null)
                    {
                        if (item.timestamp < v.Value.timestamp || item.seq < v.Value.seq)
                        {
                            item.chapterid = v.Value.chapterid;
                            item.number = v.Value.number;
                            item.location = v.Value.location;
                            item.timestamp = v.Value.timestamp;
                            item.seq = v.Value.seq;
                        }
                        else
                        {
                            conflicts.Add(v.Key, new WorkChapter(item));
                        }
                    }
                    else
                    {
                        table.Add(new Work (User.id, v.Key, v.Value.chapterid, v.Value.number, v.Value.location, v.Value.timestamp, v.Value.seq));
                    }
                }
                ctx.SaveChanges();
            }
            return conflicts;
        }

        /// <summary>
        /// PUT api/values/5
        /// Put a single WorkChapter
        /// </summary>
        /// <param name="id">Work id</param>
        /// <param name="value">WorkChapter to set</param>
        /// <returns>Existing item in case of conflict</returns>
        public IDictionary<long, WorkChapter> Put(long id, [FromBody]WorkChapter value)
        {
            return Post(new Dictionary<long, WorkChapter> { [id] = value});
        }

        /// <summary>
        /// DELETE api/values/5
        /// Delete a single workchapter from the data
        /// </summary>
        /// <param name="id">Workid</param>
        /// <remarks>Warning there is no way for clients to detect deletions so this wont work as expected.
        /// The next time the clients sync the workchapter will be readded to the database.</remarks>
        public void Delete(long id)
        {
            using (var ctx = new Models.Ao3TrackEntities())
            {
                var table = ctx.Works;

                var item = (from works in table
                            where works.userid == User.id && works.id == id
                            select works).AsEnumerable().FirstOrDefault();
                if (item != null)
                {
                    table.Remove(item);
                    ctx.SaveChanges();
                }
            }
        }
    }
}
