/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Flurl.Http;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PowerBiLib
{
    public class PowerBiLibApi : ContentTypeSpecificApiBase
    {
        public PowerBiConfig _config { get; set; }
        private TokenCredentials _tokenCredentials { get; set; } = null;

        public async override Task<UriBuilder> GetContentUri(string selectionGroupId, string UserName, HttpRequest thisHttpRequest)
        {
            await Task.Yield();

            string[] QueryStringItems = new string[]
            {
                $"group={selectionGroupId}",
            };

            UriBuilder powerBiContentUri = new UriBuilder
            {
                Scheme = thisHttpRequest.Scheme,
                Host = thisHttpRequest.Host.Host ?? "localhost",  // localhost is probably error in production but won't crash
                Port = thisHttpRequest.Host.Port ?? -1,
                Path = $"/AuthorizedContent/PowerBi",
                Query = string.Join("&", QueryStringItems),
            };

            return powerBiContentUri;
        }

        public PowerBiLibApi(PowerBiConfig configArg)
        {
            _config = configArg;
        }

        /// <summary>
        /// Asynchronous initializer, chainable with the constructor
        /// </summary>
        /// <returns></returns>
        public async Task<PowerBiLibApi> InitializeAsync()
        {
            try
            {
                await GetAccessTokenAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obtaining PowerBI authentication token");
            }

            return this;
        }

        /// <summary>
        /// Get all groups of the provided capacity
        /// </summary>
        /// <returns></returns>
        public async Task<IList<Group>> GetAllGroupsAsync()
        {
            using (var client = new PowerBIClient(_tokenCredentials))
            {
                return (await client.Groups.GetGroupsAsync()).Value;
            }
        }

        public async Task DeleteThisMethod()
        {
            var groups = await GetAllGroupsAsync();
            using (var client = new PowerBIClient(_tokenCredentials))
            {
                int i = 0;
                foreach (var group in groups)
                {
                    i++;
                    var x = await client.Groups.GetGroupUsersAsync(group.Id);
                }
            }
        }

        /// <summary>
        /// Get all reports for group
        /// </summary>
        /// <returns></returns>
        public async Task<List<ReportModel>> GetAllReportsOfGroupAsync(string groupName)
        {
            using (var client = new PowerBIClient(_tokenCredentials))
            {
                ODataResponseListGroup response = await client.Groups.GetGroupsAsync($"contains(name,'{groupName}')");
                Group group = response.Value.SingleOrDefault();
                ODataResponseListReport reportsOfTheGroup = await client.Reports.GetReportsInGroupAsync(group.Id);

                var returnVal = reportsOfTheGroup.Value.Select(r => new ReportModel(r)).ToList();
                return returnVal;
            }
        }

        /// <summary>
        /// Creates a group
        /// </summary>
        /// <returns>the Id of the newly created group</returns>
        public async Task<string> CreateGroupAsync(string name, string capacityId = null)
        {
            using (var client = new PowerBIClient(_tokenCredentials))
            {
                ODataResponseListCapacity allCapacities = await client.Capacities.GetCapacitiesAsync();
                Capacity targetCapacity = string.IsNullOrWhiteSpace(capacityId)
                    ? allCapacities.Value.SingleOrDefault()
                    : targetCapacity = allCapacities.Value.SingleOrDefault(c => c.Id == capacityId);

                if (targetCapacity == null)
                {
                    throw new ApplicationException("Designated Power BI capacity was not found");
                }

                Group newGroup = await client.Groups.CreateGroupAsync(new GroupCreationRequest(name), true);
                if (newGroup == null)
                {
                    throw new ApplicationException("Failed to create new Power BI group");
                }

                await client.Groups.AssignToCapacityAsync(newGroup.Id, new AssignToCapacityRequest(capacityId: targetCapacity.Id));

                return newGroup.Id;
            }
        }

        /// <summary>
        /// Import a .pbix file to PowerBI in the cloud
        /// </summary>
        /// <param name="pbixFullPath"></param>
        /// <param name="groupName">Name (not Id) of the group that the report and dataset should be assigned to.  A new group is created if a group with this name is not found</param>
        /// <param name="capacityId">Required only if both the named group does not exist and multiple capacities exists</param>
        /// <returns></returns>
        public async Task<PowerBiEmbedModel> ImportPbixAsync(string pbixFullPath, string groupName)
        {
            using (var client = new PowerBIClient(_tokenCredentials))
            {
                var groups = (await client.Groups.GetGroupsAsync($"contains(name,'{groupName}')")).Value.ToList();

                Group group = (await client.Groups.GetGroupsAsync($"contains(name,'{groupName}')")).Value.SingleOrDefault();
                if (group == null)
                {
                    ODataResponseListCapacity allCapacities = await client.Capacities.GetCapacitiesAsync();
                    Capacity capacity = allCapacities.Value.SingleOrDefault(c => c.Id == _config.PbiCapacityId) ?? allCapacities.Value.Single();

                    group = await client.Groups.CreateGroupAsync(new GroupCreationRequest(groupName), true);
                    if (group == null)
                    {
                        string msg = $"Requested group <{groupName}> not found and could not be created";
                        throw new ApplicationException(msg);
                    }
                    await client.Groups.AssignToCapacityAsync(group.Id, new AssignToCapacityRequest(capacityId: capacity.Id));
                }

                string pbixFileName = Path.GetFileName(pbixFullPath);
                DateTime now = DateTime.UtcNow;
                string remoteFileName = pbixFileName.Contains('_')
                                      ? pbixFileName
                                      : Path.GetFileNameWithoutExtension(pbixFileName) + $"_{now.ToString("yyyyMMdd\\ZHHmmss")}{Path.GetExtension(pbixFileName)}";

                // Initiate pbix upload and poll for completion
                Import import = await client.Imports.PostImportWithFileAsyncInGroup(group.Id, new FileStream(pbixFullPath, FileMode.Open), remoteFileName);
                while (import.ImportState != "Succeeded" && import.ImportState != "Failed")
                {
                    Thread.Sleep(500);
                    import = await client.Imports.GetImportByIdAsync(import.Id);
                }

                PowerBiEmbedModel embedProperties = (import.ImportState == "Succeeded" && import.Reports.Count == 1)
                    ? new PowerBiEmbedModel { WorkspaceId = group.Id,
                                              ReportId = import.Reports.ElementAt(0).Id,
                                              EmbedUrl = import.Reports.ElementAt(0).EmbedUrl }
                    : null;

                return embedProperties;
            }
        }

        /// <summary>
        /// Exports a report from PowerBi
        /// </summary>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<(ReportModel report, string reportFilePath)> ExportReportAsync(string groupId, string reportId, string outputFolderFullPath, bool writeFiles)
        {
            try
            {
                Guid.Parse(reportId);  // throw if null or malformed

                using (var client = new PowerBIClient(_tokenCredentials))
                {
                    Report foundReport = await client.Reports.GetReportInGroupAsync(groupId, reportId);
                    if (foundReport == null || !Guid.TryParse(foundReport.DatasetId, out _))
                    {
                        Log.Error($"From PowerBiLibApi.DeleteReport, requested report <{reportId}> not found, or related dataset Id not found");
                        return (null, null);
                    }

                    string fullOutputFilePath = Path.ChangeExtension(Path.Combine(outputFolderFullPath, foundReport.Name), "pbix");

                    Stream exportStream = await client.Reports.ExportReportAsync(foundReport.Id);
                    using (BinaryReader reader = new BinaryReader(exportStream))
                    {
                        using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(fullOutputFilePath)))
                        {
                            for (;;)
                            {
                                byte[] buffer = reader.ReadBytes(65_536);
                                if (buffer.Length > 0)
                                {
                                    if (writeFiles)
                                    {
                                        writer.Write(buffer);
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        return (new ReportModel(foundReport), writeFiles ? fullOutputFilePath : string.Empty);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"Exception from PowerBiLibApi.ExportReportAsync with request for: groupId {groupId}, reportId {reportId}");
                return (null, null);
            }
        }

        /// <summary>
        /// Deletes a report and associated dataset from PowerBi
        /// </summary>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<bool> DeleteReportAsync(string reportId)
        {
            try
            {
                Guid.Parse(reportId);  // throw if null or malformed

                using (var client = new PowerBIClient(_tokenCredentials))
                {
                    Report foundReport = await client.Reports.GetReportAsync(reportId);
                    if (foundReport == null || !Guid.TryParse(foundReport.DatasetId, out _))
                    {
                        Log.Error($"From PowerBiLibApi.DeleteReport, requested report <{reportId}> not found, or related dataset Id not found");
                        return false;
                    }
                    // Deleting the associated dataset deletes **all** reports linked to the dataset
                    object datasetDeleteResultObj = await client.Datasets.DeleteDatasetByIdAsync(foundReport.DatasetId);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"From PowerBiLibApi.DeleteReport, exception:");
                return false;
            }
            return true;
        }

        public async Task<string> GetEmbedTokenAsync(string groupId, string reportId)
        {
            // Create a Power BI Client object. it's used to call Power BI APIs.
            using (var client = new PowerBIClient(_tokenCredentials))
            {
                var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                EmbedToken tokenResponse = await client.Reports.GenerateTokenInGroupAsync(groupId, reportId, generateTokenRequestParameters);
                return tokenResponse.Token;
            }
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
                    Log.Warning("Invalid response when authenticating to PowerBI, response object is {@response}", response);
                    response = null;
                }
            }
            #region exception handling
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception attempting to get PowerBI access token");
                throw;
            }
            #endregion

            return false;
        }

        class MicrosoftAuthenticationResponse
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

    public class ReportModel
    {
        public ReportModel(Report report)
        {
            ReportId = report.Id;
            ReportName = report.Name;
        }

        public string ReportId { get; set; }

        public string ReportName { get; set; }
    }

    public class GroupModel
    {
        public GroupModel(Group group)
        {
            GroupId = group.Id;
            GroupName = group.Name;
        }

        public string GroupId { get; set; }

        public string GroupName { get; set; }
    }
}


