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
using System.Web;
using System.Security.Principal;
using Ao3tracksync.Models;

namespace Ao3tracksync.Auth
{
    public class UserPrincipal : IPrincipal
    {
        public UserIdentity Identity { get; private set; }
        public User User { get { return Identity.User; } }

        public string[] Roles { get; private set; }

        public UserPrincipal(UserIdentity ident)
        {
            this.Identity = ident;
            this.Roles = Identity.Roles.Split(',');
        }

        IIdentity IPrincipal.Identity
        {
            get { return Identity; }
        }

        bool IPrincipal.IsInRole(string role)
        {
            return Roles.Contains(role.ToLowerInvariant());
        }
    }
}
