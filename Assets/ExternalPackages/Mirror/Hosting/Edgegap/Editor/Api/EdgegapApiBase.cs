using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Edgegap.Codice.Utils;
using UnityEngine;

// using Codice.Utils; // MIRROR CHANGE
// MIRROR CHANGE

namespace Edgegap.Editor.Api
{
    /// <summary>
    ///     Handles base URL and common methods for all Edgegap APIs.
    /// </summary>
    public abstract class EdgegapApiBase
    {
        /// <param name="apiEnvironment">"console" || "staging-console"?</param>
        /// <param name="apiToken">Without the "token " prefix, although we'll clear this if present</param>
        /// <param name="logLevel">You may want more-verbose logs other than errs</param>
        protected EdgegapApiBase(
            ApiEnvironment apiEnvironment,
            string apiToken,
            EdgegapWindowMetadata.LogLevel logLevel = EdgegapWindowMetadata.LogLevel.Error
        )
        {
            SelectedApiEnvironment = apiEnvironment;

            _httpClient.BaseAddress = new Uri($"{GetBaseUrl()}/");
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );

            var cleanedApiToken = apiToken.Replace("token ", ""); // We already prefixed token below
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "token",
                cleanedApiToken
            );

            LogLevel = logLevel;
        }

        #region Vars

        private readonly HttpClient _httpClient = new(); // Base address set // MIRROR CHANGE: Unity 2020 support
        protected ApiEnvironment SelectedApiEnvironment { get; }
        protected EdgegapWindowMetadata.LogLevel LogLevel { get; set; }
        protected bool IsLogLevelDebug => LogLevel == EdgegapWindowMetadata.LogLevel.Debug;

        /// <summary>Based on SelectedApiEnvironment.</summary>
        /// <returns></returns>
        private string GetBaseUrl()
        {
            return SelectedApiEnvironment == ApiEnvironment.Staging
                ? ApiEnvironment.Staging.GetApiUrl()
                : ApiEnvironment.Console.GetApiUrl();
        }

        #endregion // Vars

        #region HTTP Requests

        /// <summary>
        ///     POST | We already added "https://api.edgegap.com/" (or similar) BaseAddress via constructor.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="json">Serialize to your model via Newtonsoft</param>
        /// <returns>
        ///     - Success => returns HttpResponseMessage result
        ///     - Error => Catches errs => returns null (no rethrow)
        /// </returns>
        protected async Task<HttpResponseMessage> PostAsync(
            string relativePath = "",
            string json = "{}"
        )
        {
            var stringContent = CreateStringContent(json);
            var uri = new Uri(_httpClient.BaseAddress, relativePath); // Normalize POST uri: Can't end with `/`.

            if (IsLogLevelDebug)
                Debug.Log($"PostAsync to: `{uri}` with json: `{json}`");

            try
            {
                return await ExecuteRequestAsync(() => _httpClient.PostAsync(uri, stringContent));
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }

        /// <summary>
        ///     PATCH | We already added "https://api.edgegap.com/" (or similar) BaseAddress via constructor.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="json">Serialize to your model via Newtonsoft</param>
        /// <returns>
        ///     - Success => returns HttpResponseMessage result
        ///     - Error => Catches errs => returns null (no rethrow)
        /// </returns>
        protected async Task<HttpResponseMessage> PatchAsync(
            string relativePath = "",
            string json = "{}"
        )
        {
            var stringContent = CreateStringContent(json);
            var uri = new Uri(_httpClient.BaseAddress, relativePath); // Normalize PATCH uri: Can't end with `/`.

            if (IsLogLevelDebug)
                Debug.Log($"PatchAsync to: `{uri}` with json: `{json}`");

            // (!) As of 11/15/2023, .PatchAsync() is "unsupported by Unity" -- so we manually set the verb and SendAsync()
            // Create the request manually
            var patchRequest = new HttpRequestMessage(new HttpMethod("PATCH"), uri)
            {
                Content = stringContent
            };

            try
            {
                return await ExecuteRequestAsync(() => _httpClient.SendAsync(patchRequest));
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }

        /// <summary>
        ///     GET | We already added "https://api.edgegap.com/" (or similar) BaseAddress via constructor.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="customQuery">
        ///     To append to the URL; eg: "foo=0&bar=1"
        ///     (!) First query key should prefix nothing, as shown
        /// </param>
        /// <returns>
        ///     - Success => returns HttpResponseMessage result
        ///     - Error => Catches errs => returns null (no rethrow)
        /// </returns>
        protected async Task<HttpResponseMessage> GetAsync(
            string relativePath = "",
            string customQuery = ""
        )
        {
            var completeRelativeUri = prepareEdgegapUriWithQuery(relativePath, customQuery);

            if (IsLogLevelDebug)
                Debug.Log($"GetAsync to: `{completeRelativeUri} with customQuery: `{customQuery}`");

            try
            {
                return await ExecuteRequestAsync(() => _httpClient.GetAsync(completeRelativeUri));
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }

        /// <summary>
        ///     DELETE | We already added "https://api.edgegap.com/" (or similar) BaseAddress via constructor.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="customQuery">
        ///     To append to the URL; eg: "foo=0&bar=1"
        ///     (!) First query key should prefix nothing, as shown
        /// </param>
        /// <returns>
        ///     - Success => returns HttpResponseMessage result
        ///     - Error => Catches errs => returns null (no rethrow)
        /// </returns>
        protected async Task<HttpResponseMessage> DeleteAsync(
            string relativePath = "",
            string customQuery = ""
        )
        {
            var completeRelativeUri = prepareEdgegapUriWithQuery(relativePath, customQuery);

            if (IsLogLevelDebug)
                Debug.Log(
                    $"DeleteAsync to: `{completeRelativeUri} with customQuery: `{customQuery}`"
                );

            try
            {
                return await ExecuteRequestAsync(() => _httpClient.DeleteAsync(completeRelativeUri)
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }

        /// <summary>POST || GET</summary>
        /// <param name="requestFunc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<HttpResponseMessage> ExecuteRequestAsync(
            Func<Task<HttpResponseMessage>> requestFunc,
            CancellationToken cancellationToken = default
        )
        {
            HttpResponseMessage response = null;
            try
            {
                response = await requestFunc();
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"HttpRequestException: {e.Message}");
                return null;
            }
            catch (TaskCanceledException e)
            {
                if (cancellationToken.IsCancellationRequested)
                    Debug.LogError("Task was cancelled by caller.");
                else
                    Debug.LogError($"TaskCanceledException: Timeout - {e.Message}");
                return null;
            }
            catch (Exception e) // Generic exception handler
            {
                Debug.LogError($"Unexpected error occurred: {e.Message}");
                return null;
            }

            // Check for a successful status code
            if (response == null)
            {
                Debug.Log("Error: (null response) - returning 500");
                return CreateUnknown500Err();
            }

            if (!response.IsSuccessStatusCode)
            {
                var httpMethod = response.RequestMessage.Method;
                Debug.Log(
                    $"Error: {(short)response.StatusCode} {response.ReasonPhrase} - "
                    + $"{httpMethod} | {response.RequestMessage.RequestUri}` - "
                    + $"{response.Content?.ReadAsStringAsync().Result}"
                );
            }

            return response;
        }

        #endregion // HTTP Requests


        #region Utils

        /// <summary>Creates a UTF-8 encoded application/json + json obj</summary>
        /// <param name="json">Arbitrary json obj</param>
        /// <returns></returns>
        private StringContent CreateStringContent(string json = "{}")
        {
            return new StringContent(json, Encoding.UTF8, "application/json");
            // MIRROR CHANGE: 'new()' not supported in Unity 2020
        }

        private static HttpResponseMessage CreateUnknown500Err()
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            // 500 - Unknown // MIRROR CHANGE: 'new()' not supported in Unity 2020
        }

        /// <summary>
        ///     Merges Edgegap-required query params (source) -> merges with custom query -> normalizes.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="customQuery"></param>
        /// <returns></returns>
        private string prepareEdgegapUriWithQuery(string relativePath, string customQuery)
        {
            // Create UriBuilder using the BaseAddress
            var uriBuilder = new UriBuilder(_httpClient.BaseAddress);

            // Add the relative path to the UriBuilder's path
            uriBuilder.Path += relativePath;

            // Parse the existing query from the UriBuilder
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            // Add default "source=unity" param
            query["source"] = "unity";

            // Parse and merge the custom query parameters
            var customParams = HttpUtility.ParseQueryString(customQuery);
            foreach (string key in customParams) query[key] = customParams[key];

            // Set the merged query back to the UriBuilder
            uriBuilder.Query = query.ToString();

            // Extract the complete relative URI and return it
            return uriBuilder.Uri.PathAndQuery;
        }

        #endregion // Utils
    }
}