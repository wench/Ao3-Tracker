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
using System.Web;
using System.Web.Http.Controllers;

namespace Ao3tracksync.Controllers
{
    /// <summary>
    /// Attribute to add to classes or methods to allow CORS access
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AllowCrossSiteAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// On execution of action, check and add required headers
        /// </summary>
        /// <param name="ActionContext"></param>
        public override void OnActionExecuted(HttpActionExecutedContext ActionContext)
        {
            var origin = "https://wenchy.net";
            if (ActionContext.Request.Headers.Contains("Origin"))
            {
                var url = ActionContext.Request.Headers.GetValues("Origin").FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    if (uri.Host == "wenchy.net" || uri.Scheme.Contains("extension"))
                        origin = url;
                }
            }
            if (ActionContext.Response.Headers.Contains("Access-Control-Allow-Origin"))
                ActionContext.Response.Headers.Remove("Access-Control-Allow-Origin");
            ActionContext.Response.Headers.Add("Access-Control-Allow-Origin", origin);
            ActionContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");

            base.OnActionExecuted(ActionContext);
        }
    }

    /// <summary>
    /// Attribute for options method containing allowed CORS access typtes
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class CrossSiteOptionsAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// On execution of action, add required headers
        /// </summary>
        /// <param name="ActionContext"></param>
        public override void OnActionExecuted(HttpActionExecutedContext ActionContext)
        {
            if (!ActionContext.Response.Headers.Contains("Access-Control-Allow-Origin"))
                ActionContext.Response.Headers.Add("Access-Control-Allow-Origin", "https://wenchy.net");
            ActionContext.Response.Headers.Add("Access-Control-Allow-Headers", "Authorization, Content-Type");
            ActionContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, OPTIONS, DELETE");
            ActionContext.Response.Headers.Add("Access-Control-Max-Age", "604800"); // 1 week

            base.OnActionExecuted(ActionContext);
        }
    }
}