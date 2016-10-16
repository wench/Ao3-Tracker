using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using Ao3tracksync;
using Ao3tracksync.Models;
using Ao3tracksync.Controllers;
public class PreWarmCache : System.Web.Hosting.IProcessHostPreloadClient
{
    public void Preload(string[] parameters)
    {
        using (var ctx = new Ao3TrackEntities())
        {
            if (!UserController.Initialized || !ValuesController.Initialized)
            {
                throw new ApplicationException("Failed to initialize application");
            }
        }
    }
}
