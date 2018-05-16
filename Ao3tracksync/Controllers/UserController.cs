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
using System.Security.Cryptography;
using System.Text;
using System.Data;
using System.Data.Linq;
using System.Web.UI;
using System.Web;

namespace Ao3tracksync.Controllers
{
    [RoutePrefix("api/User"), Authorize(Roles = "users"), AllowCrossSite, System.Web.Mvc.OutputCache(Location = OutputCacheLocation.None)]
    public class UserController : ApiController
    {
        public const int MIN_PW_SIZE = 6;
        public const string PW_SIZE_MSG = "Password must be at least 6 characters long";

        static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }


        public static bool Initialized { get; private set; }
        static UserController() { Initialized = true; }

        #region GET api/User/Init
        [AllowAnonymous, HttpGet, Route("Init")]
        public void Init()
        {
            using (var ctx = new Models.Ao3TrackEntities())
            {

            }
        }
        #endregion

        #region OPTIONS api/User
        [AllowAnonymous, CrossSiteOptions]
        public void Options()
        {
        }
        #endregion

        #region POST: api/User/Login
        public struct UserCredentials
        {
            public string username { get; set; }
            public string password { get; set; }
        }

        [AllowAnonymous, HttpPost, Route("Login")]
        public object Login([FromBody]UserCredentials credentials)
        {
            Dictionary<string, string> errors = new Dictionary<string, string> { };

            if (string.IsNullOrWhiteSpace(credentials.username)) errors.Add("username", "username must not be empty");
            if (string.IsNullOrWhiteSpace(credentials.password)) errors.Add("password", "password must not be empty");

            if (errors.Count != 0) return errors;

            var user = Models.User.GetUserWithPassword(credentials.username,credentials.password);

            if (user == null) errors.Add("password","Incorrect password.");

            if (errors.Count != 0) return errors;
            else return Auth.HashHandler.GetHashString(user.hash);
        }
        #endregion

        #region POST: api/User/Create
        public struct NewUser
        {
            public string username { get; set; }
            public string email { get; set; }
            public string password { get; set; }
        }

        [AllowAnonymous, HttpPost, Route("Create")]
        public object Create([FromBody]NewUser newuser)
        {
            using (var ctx = new Models.Ao3TrackEntities())
            {
                var Users = ctx.Users;

                Dictionary<string, string> errors = new Dictionary<string, string> { };

                // Verify username
                if (string.IsNullOrWhiteSpace(newuser.username)) errors.Add("username", "username must not be empty");
                else if (newuser.username.Contains("\n")) errors.Add("username", "username must not contain new line characters");
                else if (newuser.username.Trim() != newuser.username) errors.Add("username", "username must not start or end with whitespace");
                else if (Users.Where(u => u.username == newuser.username).Count() != 0) errors.Add("username", "username already exists");

                // Verify password
                if (string.IsNullOrWhiteSpace(newuser.password)) errors.Add("password", "password must not be empty");
                else if (newuser.password.Length < MIN_PW_SIZE) errors.Add("password", PW_SIZE_MSG);

                // Verify email address
                if (string.IsNullOrWhiteSpace(newuser.email)) newuser.email = null;
                else if (!IsValidEmail(newuser.email)) errors.Add("email", "email is not a valid email address");
                else if (Users.Where(u => u.email == newuser.email).Count() != 0) errors.Add("email", "email already used");

                // We look good to go
                if (errors.Count != 0) return errors;

                Models.User user = new Models.User(newuser.username, newuser.email, "users", newuser.password);
                Users.Add(user);
                ctx.SaveChanges();
                return Auth.HashHandler.GetHashString(user.hash);
            }
        }
        #endregion

        // PUT: api/User/5
        /*
        public void Put(int id, [FromBody]string value)
        {
        }
        */

        // DELETE: api/User/5
        /*
        public void Delete(int id)
        {
        }
        */
    }
}
