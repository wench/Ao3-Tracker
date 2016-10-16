using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace Ao3tracksync.Models
{
    public class Ao3TrackContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Work> Works { get; set; }
    }
}