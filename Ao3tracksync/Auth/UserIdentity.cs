using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using Ao3tracksync.Models;

namespace Ao3tracksync.Auth
{
    public class UserIdentity : IIdentity
    {
        public User User { get; private set; }

        public UserIdentity(User user)
        {
            this.User = user;
        }

        string IIdentity.AuthenticationType { get { return "Ao3Track"; } }

        bool IIdentity.IsAuthenticated { get { return true; } }

        string IIdentity.Name { get { return User.username; } }
    }
}

