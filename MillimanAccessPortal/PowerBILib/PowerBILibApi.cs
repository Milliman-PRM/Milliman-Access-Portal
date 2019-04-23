/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Flurl.Http;
using MapCommonLib.ContentTypeSpecific;
using Microsoft.AspNetCore.Http;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PowerBiLib
{
    public class PowerBiLibApi : ContentTypeSpecificApiBase
    {
        private PowerBiConfig _config { get; set; }
        private TokenCredentials _tokenCredentials { get; set; }

        public async override Task<UriBuilder> GetContentUri(string SelectionGroupUrl, string UserName, object ConfigInfo, HttpRequest thisHttpRequest)
        {
            await Task.Yield();
            throw new NotImplementedException();
        }

        public PowerBiLibApi(PowerBiConfig configArg = null)
        {
            if (configArg != null)
            {
                _config = configArg;
            }
        }

        /// <summary>
        /// Chainable
        /// </summary>
        /// <returns></returns>
        public async Task<PowerBiLibApi> Initialize()
        {
            await GetAccessTokenAsync();

            return this;
        }

        public async Task Demonstrate(string pbixPath)
        {
            // Create a Power BI Client object. it's used to call Power BI APIs.
            using (var client = new PowerBIClient(_tokenCredentials))
            {
                var features = client.AvailableFeatures.GetAvailableFeatures();
                var baseUri = client.BaseUri;  // returns "https://api.powerbi.com/"
                var capacities = await client.Capacities.GetCapacitiesAsync();  // returns collection of one, with name "mapbidev"

                ODataResponseListGroup allGroupsAtStart = client.Groups.GetGroups();  // This works too
                Console.WriteLine($"Before add {allGroupsAtStart.Value.Count} group names are: {string.Join(",", allGroupsAtStart.Value.Select(g=>g.Name))}");

                Capacity capacity = capacities.Value.Single();
                Group newGroup = await client.Groups.CreateGroupAsync(new GroupCreationRequest("My new group name"), true);
                var assignReturnObj = await client.Groups.AssignToCapacityAsync(newGroup.Id, new AssignToCapacityRequest(capacity.Id));

                ODataResponseListGroup allGroupsAfterCreateNew = await client.Groups.GetGroupsAsync();
                Console.WriteLine($"After add, {allGroupsAfterCreateNew.Value.Count} group names are: {string.Join(",", allGroupsAfterCreateNew.Value.Select(g => g.Name))}");

                var deleteReturnObj = await client.Groups.DeleteGroupAsync(newGroup.Id);

                ODataResponseListGroup allGroupsAfterDelete = await client.Groups.GetGroupsAsync();
                Console.WriteLine($"After del, {allGroupsAfterDelete.Value.Count} group names are: {string.Join(",", allGroupsAfterDelete.Value.Select(g => g.Name))}");

                // group/workspace stuff
                ODataResponseListGroup filteredGroups = client.Groups.GetGroups("contains(name,'MAP Dev')");
                Group lyncCanBeUsed = allGroupsAfterDelete.Value.SingleOrDefault(g => g.Name == "MAP Dev");

                // report/dataset stuff
                Report oneReport = default;
                Group oneGroup = default;
                foreach (var group in allGroupsAfterDelete.Value)
                {
                    var datasetsOfOneGroup = client.Datasets.GetDatasets(group.Id);  // none
                    var reportsOfOneGroup = client.Reports.GetReports(group.Id);
                    if (group.Name == "MAP Dev")
                    {
                        oneReport = reportsOfOneGroup.Value[0];
                        oneGroup = group;
                    }
                    Console.WriteLine($"For group <{group.Name}>:{Environment.NewLine}" +
                        $"\tfound {reportsOfOneGroup.Value.Count}  reports with names {string.Join(",", reportsOfOneGroup.Value.Select(r=>r.Name))},{Environment.NewLine}" +
                        $"\tfound {datasetsOfOneGroup.Value.Count} datasets with names {string.Join(",", datasetsOfOneGroup.Value.Select(r => r.Name))}");
                }

                var reportByReportId = client.Reports.GetReport(oneReport.Id);
                var reportByGroupAndReportId =  client.Reports.GetReport(oneGroup.Id, oneReport.Id);

                // publishing (importing)
                string fileName = Path.GetFileName(pbixPath);
                DateTime now = DateTime.UtcNow;
                string remoteFileName = Path.GetFileNameWithoutExtension(fileName) + $"_{now.ToString("yyyyMMdd\\ZHHmmss")}{Path.GetExtension(fileName)}";
                Import import = await client.Imports.PostImportWithFileAsyncInGroup(
                    oneGroup.Id, 
                    new FileStream(pbixPath, FileMode.Open), 
                    remoteFileName);
                while (import.ImportState != "Succeeded" && import.ImportState != "Failed")
                {
                    Console.WriteLine("Waiting for import completion, last import object is {0}", JsonConvert.SerializeObject(import));
                    Thread.Sleep(500);
                    import = await client.Imports.GetImportByIdAsync(import.Id);
                }

                // embedded viewing
                // Read in detail: https://docs.microsoft.com/en-us/power-bi/developer/embed-sample-for-customers
                //  in particular: https://docs.microsoft.com/en-us/power-bi/developer/embed-sample-for-customers#load-an-item-using-javascript
                var tokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                EmbedToken tokenResponse = client.Reports.GenerateTokenInGroup(oneGroup.Id, oneReport.Id, tokenRequestParameters);
                // TODO: Generate embed model for Razor view

                // Delete report/dataset
                await client.Reports.DeleteReportInGroupAsync(oneGroup.Id, import.Reports[0].Id);
                await client.Datasets.DeleteDatasetByIdInGroupAsync(oneGroup.Id, import.Datasets[0].Id);
                // Note that deleting the dataset while the report exists causes both to be deleted. 
            }
        }

        /// <summary>
        /// Initialize a new access token
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GetAccessTokenAsync()
        {
            // It may be possible to replace this with something that uses package:  Microsoft.IdentityModel.Clients.ActiveDirectory
            // Microsoft has deprecated the class 
            try
            {
                var response = await _config.PbiTokenEndpoint
                        .PostMultipartAsync(mp => mp
                            .AddString("grant_type", _config.PbiGrantType)
                            .AddString("scope", _config.PbiAuthenticationScope)
                            .AddString("client_id", _config.PbiAzureADClientId)
                            .AddString("client_secret", _config.PbiAzureADClientSecret)
                            .AddString("username", _config.PbiAzureADUsername)
                            .AddString("password", _config.PbiAzureADPassword)
                        )
                        .ReceiveJson<MicrosoftAuthenticationResponse>();

                if (response.ExpiresIn > 0 && response.ExtExpiresIn > 0)
                {
                    _tokenCredentials = new TokenCredentials(response.AccessToken, response.TokenType);
                    return true;
                }
                else
                {
                    // The response was converted to a MicrosoftAuthenticationResponse, but seems to be invalid
                    // TODO: Do some real logging here
                    response = null;
                }
            }
            #region exception handling
            catch (FlurlHttpTimeoutException ex)
            {
            }
            catch (FlurlHttpException ex)
            {
            }
            catch (Exception ex)
            {
                // TODO: Do some real logging here
            }
            #endregion

            return false;
        }

        public class MicrosoftAuthenticationResponse
        {
            [JsonProperty(PropertyName = "token_type")]
            public string TokenType { set; internal get; }

            [JsonProperty(PropertyName = "scope")]
            public string Scope { set; internal get; }

            [JsonProperty(PropertyName = "expires_in")]
            public int ExpiresIn { set; internal get; }

            [JsonProperty(PropertyName = "ext_expires_in")]
            public int ExtExpiresIn { set; internal get; }

            [JsonProperty(PropertyName = "access_token")]
            public string AccessToken { set; internal get; }
        }
    }
}


