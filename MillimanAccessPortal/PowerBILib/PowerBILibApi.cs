using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MapCommonLib.ContentTypeSpecific;
using Microsoft.AspNetCore.Http;
using Flurl;
using Flurl.Http;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PowerBILib
{
    public class PowerBILibApi : ContentTypeSpecificApiBase
    {
        public PowerBIConfig config { get; set; }

        // Store the Azure AD authentication token to be used by other calls
        public MicrosoftAuthenticationResponse authToken { get; set; }

        /// <summary>
        /// Private parameterless constructor blocks its usage - the config object is strictly required
        /// </summary>
        private PowerBILibApi() { }

        /// <summary>
        /// Build a new API client using the supplied configuration
        /// </summary>
        /// <param name="configArg"></param>
        public PowerBILibApi(PowerBIConfig configArg)
        {
            config = configArg;
        }

        /// <summary>
        /// Initialize a new access token
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GetAccessTokenAsync()
        {
            try
            {
                var response = await config.pbiTokenEndpoint
                        .PostMultipartAsync(mp => 
                            mp.AddString("grant_type", config.pbiGrantType)
                                .AddString("scope", config.pbiAuthenticationScope)
                                .AddString("client_id", config.pbiAzureADClientId)
                                .AddString("client_secret", config.pbiAzureADClientSecret)
                                .AddString("username", config.pbiAzureADUsername)
                                .AddString("password", config.pbiAzureADPassword)
                            )
                        .ReceiveJson<MicrosoftAuthenticationResponse>();

                if (response.expires_in > 0 && response.ext_expires_in > 0)
                {
                    authToken = response;
                }
                else
                {
                    // The response was successfully converted to a MicrosoftAuthenticationResponse, but seems to be invalid
                    // TODO: Do some real logging here
                    authToken = null;
                    return false;
                }
            }
            catch (FlurlHttpTimeoutException ex)
            {
                return false;
            }
            catch (FlurlHttpException ex)
            {
                return false;
            }
            catch (Exception ex)
            {
                // TODO: Do some real logging here
                return false;
            }

            return true;                        
        }

        public void GetWorkspaces()
        {
            if (authToken == null)
            {
                bool gotToken = GetAccessTokenAsync().Result;
            }

            try
            {
                var response = "https://api.powerbi.com/v1.0/myorg/groups/"
                                    .WithHeader("Authorization", $"{authToken.token_type} {authToken.access_token}")
                                    .GetJsonAsync<dynamic>().Result;

                Console.WriteLine("Response: " + response.value.ToString());

            }
            catch (FlurlHttpTimeoutException ex)
            {
                return;
            }
            catch (FlurlHttpException ex)
            {
                return;
            }
            catch (Exception ex)
            {
                // TODO: Do some real logging here
                return;
            }
        }

        public override Task<UriBuilder> GetContentUri(string SelectionGroupUrl, string UserName, object ConfigInfo, HttpRequest thisHttpRequest)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// POCO class to simplify de-serializing the response in GetAccessTokenAsync()
    /// </summary>
    public class MicrosoftAuthenticationResponse
    {
        public string token_type { get; set; }

        public string scope { get; set; }

        public int expires_in { get; set; }

        public int ext_expires_in { get; set; }

        public string access_token { get; set; }
    }
}
