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
using System.Web;
using System.Web.Http.Controllers;

namespace Ao3tracksync.Controllers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AllowCrossSiteAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext ActionContext)
        {
            try
            {
                Uri uri = new Uri(ActionContext.Request.Headers.GetValues("Origin").First());
                if (uri.Scheme.Contains("extension"))
                    ActionContext.Response.Headers.Add("Access-Control-Allow-Origin", uri.OriginalString);
            }
            catch
            {
            }
            ActionContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");

            base.OnActionExecuted(ActionContext);
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class CrossSiteOptionsAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext ActionContext)
        {
            ActionContext.Response.Headers.Add("Access-Control-Allow-Headers", "Authorization, Content-Type");
            ActionContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, OPTIONS, DELETE");
            ActionContext.Response.Headers.Add("Access-Control-Max-Age", "604800"); // 1 week

            base.OnActionExecuted(ActionContext);
        }
    }
}