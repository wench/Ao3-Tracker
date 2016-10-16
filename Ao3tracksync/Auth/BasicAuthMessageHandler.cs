using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using Ao3tracksync.Models;

namespace Ao3tracksync.Auth
{
    public class BasicAuthMessageHandler : MessageProcessingHandler
    {
        private const string AuthenticateHeader = "WWW-Authenticate";
        private const string AuthenticateScheme = "Ao3track";

        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                AuthenticationHeaderValue authValue = request.Headers.Authorization;
                if (authValue != null && AuthenticateScheme == authValue.Scheme && !string.IsNullOrWhiteSpace(authValue.Parameter))
                {
                    string[] credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authValue.Parameter)).Split('\n');

                    if (credentials.Length == 2 && !string.IsNullOrWhiteSpace(credentials[0]) && !string.IsNullOrWhiteSpace(credentials[1]))
                    {
                        User user = User.GetUserWithHash(credentials[0], credentials[1]);
                        if (user == null)
                        {
                            return null;
                        }

                        Thread.CurrentPrincipal = HttpContext.Current.User = new UserPrincipal(new UserIdentity(user));
                    }
                }
            }
            catch (FormatException)
            {
            }
            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized && !response.Headers.Contains(AuthenticateHeader))
            {
                response.Headers.Add(AuthenticateHeader, AuthenticateScheme);
                response.Headers.Add("Access-Control-Allow-Origin", "*");
            }
            return response;
        }

    }
}
