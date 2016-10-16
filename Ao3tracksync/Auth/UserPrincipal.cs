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
            this.Roles = Identity.User.roles.ToLowerInvariant().Split(',');
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
