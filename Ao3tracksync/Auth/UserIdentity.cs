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

