using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyWebApplication
{
    /// <summary>
    /// Helper class to ensure this sample has been configured correctly.
    /// </summary>
    public static class ConfigUtil
    {
        /// <summary>
        /// Ensures this sample has been configured correctly,
        /// failing nicely depending on whether it was run from the command line or from an IDE.
        /// </summary>
        /// <param name="configuration">Application configuration instance</param>
        public static void ValidateConfiguration(IConfiguration configuration)
        {
            try
            {
                ValidatePasConnection(configuration);
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Console.WriteLine("ERROR: " + e.Message);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Verifies this sample has been configured correctly to be able to send requests to PAS (PrizmDoc Application Services). See the README.md for more info.
        /// </summary>
        /// <param name="configuration">Application configuration instance</param>
        public static void ValidatePasConnection(IConfiguration configuration)
        {
            ValidatePasConnectionAsync(configuration).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously verifies this sample has been configured correctly to be able to send requests to PAS (PrizmDoc Application Services). See the README.md for more info.
        /// </summary>
        /// <param name="configuration">Application configuration instance</param>
        public static async Task ValidatePasConnectionAsync(IConfiguration configuration)
        {
            // This method tests creating a viewing session using the current application
            // configuration. If anything fails, we throw an exception with a helpful
            // error message so that, if you're just getting started with this sample,
            // you'll have some idea of what's wrong and what you should do to fix your
            // configuration to PAS (PrizmDoc Application Services).
            //
            // Don't be overwhelmed by the code in this method.
            // We're just trying to make your sample setup experience nice.
            // In a production application, you could remove all of this.

            if (configuration["PrizmDoc:PasBaseUrl"] == null)
            {
                throw new Exception("Missing required configuration setting \"PrizmDoc:PasBaseUrl\". See the README.md for more information.");
            }

            const string defaultPasBaseUrl = "https://api.accusoft.com/prizmdoc/";
            const string defaultCloudApiKey = "YOUR_API_KEY";
            const string defaultPasSecretKey = null;

            var configurationHasNotYetBeenSet = configuration["PrizmDoc:PasBaseUrl"] == defaultPasBaseUrl && configuration["PrizmDoc:CloudApiKey"] == defaultCloudApiKey && configuration["PrizmDoc:PasSecretKey"] == defaultPasSecretKey;

            if (configurationHasNotYetBeenSet)
            {
                throw new Exception("It looks like you have not yet configured your connection to PAS (PrizmDoc Application Services). " +
                                    "See the README.md for more information.");
            }

            var pasHttpClient = PasUtil.CreatePasHttpClient(configuration);

            // Test POST /ViewingSession to see whether the configuration will work or not
            var response = await pasHttpClient.PostAsync("ViewingSession", new StringContent(JsonConvert.SerializeObject(new
            {
                source = new
                {
                    type = "upload",
                    displayName = "test"
                }
            })));
            var json = await response.Content.ReadAsStringAsync();
            string errorCode = null;

            if (!response.IsSuccessStatusCode)
            {
                // Parse the JSON errorCode, if present
                try
                {
                    errorCode = (string)JObject.Parse(json)["errorCode"];
                }
                catch (JsonReaderException)
                {
                    // Ignore JSON parsing errors. errorCode will remain null.
                }
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized && errorCode == "Unauthorized")
            {
                var valueSet = configuration["PrizmDoc:CloudApiKey"] != null;

                if (valueSet)
                {
                    throw new Exception("Invalid API key. Make sure your \"PrizmDoc:CloudApiKey\" configuration value is correct. " +
                                        "See the README.md for more information.");
                }
                else
                {
                    throw new Exception("Missing API key. Make sure you have provided a \"PrizmDoc:CloudApiKey\" configuration setting. " +
                                        "Visit https://cloud.accusoft.com to sign up for an account and get an API key at no cost. " +
                                        "See the README.md for more information.");
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Unexpected response when testing the connection to PAS. Are you sure your \"PrizmDoc:PasBaseUrl\" configuration setting is correct? " +
                                    "See the README.md for more information.");
            }

            // Test PUT /SourceFile, for the self-hosted case, to make sure the secret key will work
            var viewingSessionId = (string)JObject.Parse(json)["viewingSessionId"];
            var route = $"ViewingSession/u{viewingSessionId}/SourceFile?FileExtension=txt";
            var content = new StringContent("test");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            response = await pasHttpClient.PutAsync(route, content);
            json = await response.Content.ReadAsStringAsync();

            errorCode = null;
            if (!response.IsSuccessStatusCode)
            {
                // Parse the JSON errorCode, if present
                try
                {
                    errorCode = (string)JObject.Parse(json)["errorCode"];
                }
                catch (JsonReaderException)
                {
                    // Ignore JSON parsing errors. errorCode will remain null.
                }
            }

            if (response.StatusCode == HttpStatusCode.Forbidden && errorCode == "InvalidSecret")
            {
                var valueSet = configuration["PrizmDoc:PasSecretKey"] != null;

                if (valueSet)
                {
                    throw new Exception("Invalid PAS secret. Make sure your \"PrizmDoc:PasSecret\" configuration value matches the \"secretKey\" value in your PAS config file. " +
                                        "See the README.md for more information.");
                }
                else
                {
                    throw new Exception("You appear to be using a self-hosted PAS instance, but you have not provided a \"PrizmDoc:PasSecret\" configuration setting. " +
                                        "When self-hosting PAS, certain requests require an \"Accusoft-Secret\" header in order to be accepted. " +
                                        "To ensure this header is correctly sent, make sure to provide a \"PrizmDoc:PasSecret\" configuration value " +
                                        "which matches the \"secretKey\" value in your PAS config file. " +
                                        "See the README.md for more information.");
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Unexpected response when testing the connection to PAS. " +
                                    "Have you configured the connection to PAS (PrizmDoc Application Services) correctly? " +
                                    "See the README.md for more information.");
            }
        }
    }
}
