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

        public class WorkChapter
        {
            public WorkChapter()
            {
            }
            public WorkChapter(Work work)
            {
                if (work != null)
                {
                    chapterid = work.chapterid;
                    number = work.number;
                    location = work.location;
                    timestamp = work.timestamp;
                }
            }
            public long chapterid { get; set; }
            public long number { get; set; }
            public long? location { get; set; }
            public long timestamp { get; set; }
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

        // GET api/values
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

        // GET api/values/5
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


        // POST api/values
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
                        if (item.timestamp < v.Value.timestamp)
                        {
                            item.chapterid = v.Value.chapterid;
                            item.number = v.Value.number;
                            item.location = v.Value.location;
                            item.timestamp = v.Value.timestamp;
                        }
                        else
                        {
                            conflicts.Add(v.Key, new WorkChapter(item));
                        }
                    }
                    else
                    {
                        table.Add(new Work (User.id, v.Key, v.Value.chapterid, v.Value.number, v.Value.location, v.Value.timestamp));
                    }
                }
                ctx.SaveChanges();
            }
            return conflicts;
        }

        // PUT api/values/5
        public IDictionary<long, WorkChapter> Put(long id, [FromBody]WorkChapter value)
        {
            return Post(new Dictionary<long, WorkChapter> { [id] = value});
        }

        // DELETE api/values/5
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
