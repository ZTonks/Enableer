// <copyright file="GraphClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GraphTeamsTag.Provider
{
    using GraphTeamsTag;
    using GraphTeamsTag.Helper;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Graph;

    /// <summary>
    /// Factory for creating GraphServiceClient instances.
    /// </summary>
    public class GraphClientFactory : IGraphClientFactory
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphClientFactory"/> class.
        /// </summary>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        public GraphClientFactory(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Creates a GraphServiceClient instance using the provided SSO token.
        /// </summary>
        /// <param name="ssoToken">The SSO token to exchange for an access token.</param>
        /// <returns>A configured GraphServiceClient instance.</returns>
        public async Task<GraphServiceClient> CreateGraphClientAsync(string ssoToken)
        {
            var token = await SSOAuthHelper.GetAccessTokenOnBehalfUserAsync(
                _configuration,
                _httpClientFactory,
                _httpContextAccessor,
                ssoToken);

            return SimpleGraphClient.GetGraphClient(token);
        }
    }
}

