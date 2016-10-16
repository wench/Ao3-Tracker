using System.Security.Principal;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Web.Hosting;

namespace Piotr.BasicHttpAuth.Web
{
    public class SQLitePrincipalProvider : IProvidePrincipal
    {
        private const string Username = "username";
        private const string Password = "password";

        IDbConnection Connection
        {
            get
            {
                DbProviderFactory fact = DbProviderFactories.GetFactory("System.Data.SQLite");
                DbConnection cnn = fact.CreateConnection();
                cnn.ConnectionString = "Data Source=" + System.IO.Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "App_Data\\workschapters.db3");
                cnn.Open();
                return cnn;
            }
        }

        public IPrincipal CreatePrincipal(string username, string password)
        {
            if (username != Username || password != Password)
            {
                return null;
            }

            var identity = new GenericIdentity(Username);
            IPrincipal principal = new GenericPrincipal(identity, new[] { "User" });
            return principal;
        }
    }
}