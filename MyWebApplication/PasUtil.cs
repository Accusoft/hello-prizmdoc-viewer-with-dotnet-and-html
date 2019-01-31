using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace MyWebApplication
{
    /// <summary>
    /// Helper class for sending HTTP requests to PAS (PrizmDoc Application Services)
    /// </summary>
    public static class PasUtil
    {
        /// <summary>
        /// Creates an instance of an HttpClient which is pre-configured to use the PAS base URL and cloud API key (or self-hosted secret key) as specified by application configuration.
        /// </summary>
        public static HttpClient CreatePasHttpClient(IConfiguration configuration)
        {
            return CreatePasHttpClient(configuration["PrizmDoc:PasBaseUrl"], configuration["PrizmDoc:CloudApiKey"], configuration["PrizmDoc:PasSecretKey"]);
        }

        /// <summary>
        /// Creates an instance of an HttpClient which is pre-configured with the provided PAS base URL, cloud API key, and self-hosted secret key.
        /// </summary>
        public static HttpClient CreatePasHttpClient(string pasBaseUrl, string cloudApiKey = null, string selfHostedPasSecretKey = null)
        {
            var httpClient = new HttpClient();

            // Ensure the base URL ends with a trailing slash
            var baseUrl = pasBaseUrl;
            if (!pasBaseUrl.EndsWith("/"))
            {
                baseUrl += "/";
            }

            httpClient.BaseAddress = new Uri(baseUrl);

            // Inject the PrizmDoc Cloud API key if one was defined
            if (cloudApiKey != null && cloudApiKey.Trim() != "")
            {
                httpClient.DefaultRequestHeaders.Add("acs-api-key", cloudApiKey);
            }

            // Inject Accusoft-Secret header for self-hosted PAS if one was defined
            if (selfHostedPasSecretKey != null && selfHostedPasSecretKey.Trim() != "")
            {
                httpClient.DefaultRequestHeaders.Add("Accusoft-Secret", selfHostedPasSecretKey);
            }

            return httpClient;
        }
    }
}
