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

        bool failedAuth = false;

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
                        if (user != null)
                        {
                            Thread.CurrentPrincipal = HttpContext.Current.User = new UserPrincipal(new UserIdentity(user));
                        }                       
                        else
                        {
                            failedAuth = true;
                        }
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

            // Oops this is to work around a bug in the client
            if (response.StatusCode == HttpStatusCode.Unauthorized && failedAuth)
            {
                response.StatusCode = HttpStatusCode.Forbidden;
            }

            return response;
        }

    }
}
