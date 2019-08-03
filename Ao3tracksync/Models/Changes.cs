using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ao3tracksync.Models
{
    public class Changes
    {
        public Dictionary<string, string> releases { get; private set; } = new Dictionary<string, string>();
        public Version from { get;  set; } = null;
        public Version to { get;  set; } = null;
    }

    public class GITHUB_RELEASE
    {
        public class User
        {
            public string login;
            public string id;
            public string node_id;
            public string avatar_url;
            public string gravatar_id;
            public string url;
            public string html_url;


            public string followers_url;
            public string following_url;
            public string gists_url;
            public string starred_url;
            public string subscriptions_url;
            public string organizations_url;
            public string repos_url;
            public string events_url;
            public string received_events_url;
            public string type;
            public bool site_admin;

        };
        public User author;
        public bool prerelease;
        public bool draft;
        public string created_at;
        public string published_at;
        public string body;
        public string name;
    }
}