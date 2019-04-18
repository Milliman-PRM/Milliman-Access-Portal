/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: An API wrapper which simplifies interacting with Power BI Embedded services
 * DEVELOPER NOTES:
 */
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
using System.IO;
using System.Threading;

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
                var response = await config.PbiTokenEndpoint
                        .PostMultipartAsync(mp => 
                            mp.AddString("grant_type", config.PbiGrantType)
                                .AddString("scope", config.PbiAuthenticationScope)
                                .AddString("client_id", config.PbiAzureADClientId)
                                .AddString("client_secret", config.PbiAzureADClientSecret)
                                .AddString("username", config.PbiAzureADUsername)
                                .AddString("password", config.PbiAzureADPassword)
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

        /// <summary>
        /// Return a list of all workspaces in the organization
        /// </summary>
        public PowerBIWorkspace[] GetWorkspaces()
        {
            if (authToken == null)
            {
                bool gotToken = GetAccessTokenAsync().Result;
            }

            try
            {
                PowerBIWorkspace[] response = "https://api.powerbi.com/v1.0/myorg/groups/"
                                    .WithHeader("Authorization", $"{authToken.token_type} {authToken.access_token}")
                                    .GetJsonAsync<dynamic>()
                                    .Result
                                    .value
                                    .ToObject<PowerBIWorkspace[]>();

                return response;

            }
            catch (FlurlHttpTimeoutException ex)
            {
                return null;
            }
            catch (FlurlHttpException ex)
            {
                return null;

            }
            catch (Exception ex)
            {
                // TODO: Do some real logging here
                return null;
            }
        }

        /// <summary>
        /// Returns a list of all reports in a specified workspace
        /// </summary>
        /// <param name="workspace"></param>
        /// <returns></returns>
        public PowerBIReport[] GetReportsInWorkspace(string workspaceID)
        {
            if (authToken == null)
            {
                bool gotToken = GetAccessTokenAsync().Result;
            }

            try
            {
                var response = $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceID}/reports/"
                                    .WithHeader("Authorization", $"{authToken.token_type} {authToken.access_token}")
                                    .GetJsonAsync<dynamic>()
                                    .Result
                                    .value
                                    .ToObject<PowerBIReport[]>();
                return response;

            }
            catch (FlurlHttpTimeoutException ex)
            {
                return null;
            }
            catch (FlurlHttpException ex)
            {
                return null;

            }
            catch (Exception ex)
            {
                // TODO: Do some real logging here
                return null;
            }
        }

        /// <summary>
        /// Get details of a specific report using its ID
        /// </summary>
        /// <param name="reportId">The ID property of the report to be located</param>
        /// <returns>Returns the located PowerBIReport, or null if it is not found</returns>
        public PowerBIReport GetReportById(string reportId)
        {
            if (string.IsNullOrEmpty(reportId))
            {
                // log an error and return null 

                return null;
            }

            // Query the API for a specific report and return it
            try
            {
                var result = $"https://api.powerbi.com/v1.0/myorg/reports/{reportId}"
                            .WithHeader("Authorization", $"{authToken.token_type} {authToken.access_token}")
                                    .GetJsonAsync<dynamic>()
                                    .Result
                                    .ToObject<PowerBIReport>();


                return result;
            }
            catch (FlurlHttpTimeoutException ex)
            {
                return null;
            }
            catch (FlurlHttpException ex)
            {
                return null;

            }
            catch (Exception ex)
            {
                // TODO: Do some real logging here
                return null;
            }
            


            // If no report was found and returned above, return null
            return null;
        }

        /// <summary>
        /// Publish a PBIX as a new report within a specified workspace
        /// </summary>
        /// <param name="path">The fully-qualified path to the PBIX file to be uploaded</param>
        /// <param name="workspaceId">The ID property of the workspace where the report should be published</param>
        /// <returns>Returns the published PowerBIReport if successful, null if not successful</returns>
        public PowerBIReport PublishPbix(string path, string workspaceId)
        {
            #region Validation
            if (string.IsNullOrEmpty(workspaceId) || string.IsNullOrEmpty(path))
            {
                // log an error and return null 

                return null;
            }

            // Test that the path is valid
            if (!File.Exists(path))
            {
                // Do something because the path that was provided is invalid

            }
            #endregion

            string fileName = Path.GetFileName(path);
            string reportId = "";

            // Perform the publish action
            try
            {
                // First, initiate the file upload
                var importJobId = $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/imports/?datasetDisplayName={fileName}&nameConflict=Abort"
                        .WithHeader("Authorization", $"{authToken.token_type} {authToken.access_token}")
                        .PostMultipartAsync(mp => mp.AddFile(fileName, new FileStream(path, FileMode.Open), fileName))
                        .ReceiveJson<dynamic>().Result.id;

                // Next, query the import job to determine when it is finished

                bool uploadInProgress = true;

                while (uploadInProgress)
                {
                    Thread.Sleep(5000);

                    var importStatus = $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/imports/{importJobId}"
                        .WithHeader("Authorization", $"{authToken.token_type} {authToken.access_token}")
                        .GetJsonAsync<dynamic>().Result;

                    if (importStatus.importState == "Succeeded")
                    {
                        reportId = importStatus.reports[0].id;
                        uploadInProgress = false;
                    }
                    else if (importStatus.importState != "Publishing")
                    {
                        // TODO: An error has occurred and needs to be handled
                        uploadInProgress = false;
                        return null;
                    }
                }

                // Finally, retrieve and return the published report
                return GetReportById(reportId);
            }
            catch (FlurlHttpTimeoutException ex)
            {
                return null;
            }
            catch (FlurlHttpException ex)
            {
                return null;

            }
            catch (Exception ex)
            {
                // TODO: Do some real logging here
                return null;
            }
            
        }

        public override Task<UriBuilder> GetContentUri(string SelectionGroupUrl, string UserName, object ConfigInfo, HttpRequest thisHttpRequest)
        {
            throw new NotImplementedException();
        }
    }

    #region Data structure classes

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

    /// <summary>
    /// A PowerBI Workspace (APIs also call these "groups")
    /// </summary>
    public class PowerBIWorkspace
    {
        public string id { get; set;  }

        public bool isReadOnly { get; set; }

        public bool isOnDedicatedCapacity { get; set; }

        public string capacityId { get; set; }

        public string name { get; set; }
    }
    
    /// <summary>
    /// A PowerBI report object
    /// </summary>
    public class PowerBIReport
    {
        public string id { get; set; }

        public string name { get; set; }

        public string webUrl { get; set; }

        public string embedUrl { get; set; }

        public bool isFromPbix { get; set; }

        public bool isOwnedByMe { get; set; }

        public string datasetId { get; set; }
    }

    #endregion
}
