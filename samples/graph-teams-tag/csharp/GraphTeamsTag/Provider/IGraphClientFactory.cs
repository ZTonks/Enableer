// <copyright file="IGraphClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GraphTeamsTag.Provider
{
    using Microsoft.Graph;

    /// <summary>
    /// Interface for creating GraphServiceClient instances.
    /// </summary>
    public interface IGraphClientFactory
    {
        /// <summary>
        /// Creates a GraphServiceClient instance using the provided SSO token.
        /// </summary>
        /// <param name="ssoToken">The SSO token to exchange for an access token.</param>
        /// <returns>A configured GraphServiceClient instance.</returns>
        Task<GraphServiceClient> CreateGraphClientAsync(string ssoToken);
    }
}

