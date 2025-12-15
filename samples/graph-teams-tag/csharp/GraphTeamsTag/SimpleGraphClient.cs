using System.Net.Http.Headers;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;

namespace GraphTeamsTag
{
    public class SimpleGraphClient
    {
        // Get graph client based on access token.
        public static GraphServiceClient GetGraphClient(string accessToken)
        {
            var authenticationProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(accessToken));
            var graphClient = new GraphServiceClient(authenticationProvider);
            return graphClient;
        }

        // Get graph client based on application configuration.
        public static GraphServiceClient GetGraphClientforApp(string appId, string appPassword, string tenantId)
        {
            var clientSecretCredential = new ClientSecretCredential(tenantId, appId, appPassword);
            var graphClient = new GraphServiceClient(clientSecretCredential);
            return graphClient;
        }
    }

    public class TokenProvider : IAccessTokenProvider
    {
        private string _accessToken;

        public TokenProvider(string accessToken)
        {
            _accessToken = accessToken;
        }

        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = default, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_accessToken);
        }

        public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator();
    }
}
