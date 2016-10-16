using System.Security.Principal;

namespace Piotr.BasicHttpAuth.Web
{
    public class DummyPrincipalProvider : IProvidePrincipal
    {
        private const string Username = "username";
        private const string Password = "password";

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