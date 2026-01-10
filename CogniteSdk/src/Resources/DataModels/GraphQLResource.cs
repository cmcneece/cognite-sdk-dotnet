// Copyright 2024 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using CogniteSdk.DataModels;

namespace CogniteSdk.Resources.DataModels
{
    /// <summary>
    /// Resource for executing GraphQL queries against CDF Data Models.
    /// </summary>
    /// <remarks>
    /// <para>
    /// GraphQL provides a flexible query interface for data models, allowing you to
    /// query across multiple views and traverse relationships in a single request.
    /// </para>
    /// <para>
    /// This resource is separate from the main SDK pipeline and uses HttpClient directly.
    /// Create an instance using the constructor and provide authentication.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var graphql = new GraphQLResource(httpClient, project, baseUrl, tokenProvider);
    /// var result = await graphql.QueryAsync&lt;MyDataType&gt;(
    ///     space: "my-space",
    ///     externalId: "my-model",
    ///     version: "1",
    ///     query: "{ listMyView { items { name } } }"
    /// );
    /// </code>
    /// </example>
    public class GraphQLResource
    {
        private readonly HttpClient _httpClient;
        private readonly string _project;
        private readonly string _baseUrl;
        private readonly Func<CancellationToken, Task<string>> _tokenProvider;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Creates a new GraphQL resource.
        /// </summary>
        /// <param name="httpClient">HTTP client to use for requests. Should be reused across requests.</param>
        /// <param name="project">CDF project name.</param>
        /// <param name="baseUrl">CDF base URL (e.g., "https://api.cognitedata.com").</param>
        /// <param name="tokenProvider">Function to get a valid access token.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public GraphQLResource(
            HttpClient httpClient,
            string project,
            string baseUrl,
            Func<CancellationToken, Task<string>> tokenProvider)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _baseUrl = (baseUrl ?? throw new ArgumentNullException(nameof(baseUrl))).TrimEnd('/');
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Executes a GraphQL query against a data model.
        /// </summary>
        /// <typeparam name="T">Type to deserialize the data to.</typeparam>
        /// <param name="space">Space containing the data model.</param>
        /// <param name="externalId">Data model external ID.</param>
        /// <param name="version">Data model version.</param>
        /// <param name="query">GraphQL query string.</param>
        /// <param name="variables">Optional query variables.</param>
        /// <param name="operationName">Optional operation name when query contains multiple operations.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>GraphQL response with typed data.</returns>
        /// <exception cref="ArgumentException">Thrown when space, externalId, version, or query is null or empty.</exception>
        /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
        public async Task<GraphQLResponse<T>> QueryAsync<T>(
            string space,
            string externalId,
            string version,
            string query,
            Dictionary<string, object> variables = null,
            string operationName = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(space))
                throw new ArgumentException("Space cannot be null or empty", nameof(space));
            if (string.IsNullOrWhiteSpace(externalId))
                throw new ArgumentException("ExternalId cannot be null or empty", nameof(externalId));
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Version cannot be null or empty", nameof(version));
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty", nameof(query));

            var request = new GraphQLRequest
            {
                Query = query,
                Variables = variables,
                OperationName = operationName
            };

            var url = $"{_baseUrl}/api/v1/projects/{_project}/userapis/spaces/{space}/datamodels/{externalId}/versions/{version}/graphql";

            var accessToken = await _tokenProvider(token).ConfigureAwait(false);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            try
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
                httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(httpRequest, token).ConfigureAwait(false);
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"GraphQL request failed: {response.StatusCode} - {responseContent}");
                }

                return JsonSerializer.Deserialize<GraphQLResponse<T>>(responseContent, _jsonOptions)
                    ?? new GraphQLResponse<T>();
            }
            finally
            {
                httpRequest.Dispose();
            }
        }

        /// <summary>
        /// Executes a GraphQL query and returns raw JSON data.
        /// </summary>
        /// <param name="space">Space containing the data model.</param>
        /// <param name="externalId">Data model external ID.</param>
        /// <param name="version">Data model version.</param>
        /// <param name="query">GraphQL query string.</param>
        /// <param name="variables">Optional query variables.</param>
        /// <param name="operationName">Optional operation name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>GraphQL response with raw JSON data.</returns>
        public async Task<GraphQLRawResponse> QueryRawAsync(
            string space,
            string externalId,
            string version,
            string query,
            Dictionary<string, object> variables = null,
            string operationName = null,
            CancellationToken token = default)
        {
            var response = await QueryAsync<JsonElement>(space, externalId, version, query, variables, operationName, token)
                .ConfigureAwait(false);
            return new GraphQLRawResponse
            {
                Data = response.Data,
                Errors = response.Errors,
                Extensions = response.Extensions
            };
        }

        /// <summary>
        /// Introspects the GraphQL schema for a data model.
        /// </summary>
        /// <param name="space">Space containing the data model.</param>
        /// <param name="externalId">Data model external ID.</param>
        /// <param name="version">Data model version.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>GraphQL response with schema information.</returns>
        public async Task<GraphQLRawResponse> IntrospectAsync(
            string space,
            string externalId,
            string version,
            CancellationToken token = default)
        {
            const string IntrospectionQuery = @"
                query IntrospectionQuery {
                    __schema {
                        types {
                            name
                            kind
                            fields {
                                name
                                type { name kind }
                            }
                        }
                    }
                }";

            return await QueryRawAsync(space, externalId, version, IntrospectionQuery, token: token)
                .ConfigureAwait(false);
        }
    }
}
