
using Flurl.Http;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PowerBILib
{
    public class PowerBiLib2
    {
        private PowerBIConfig _config { get; set; }
        private TokenCredentials _tokenCredentials { get; set; }

        public PowerBiLib2(PowerBIConfig configArg = null)
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
        public async Task<PowerBiLib2> Initialize()
        {
            await GetAccessTokenAsync();

            return this;
        }

        public async Task Demonstrate()
        {
            // Create a Power BI Client object. it's used to call Power BI APIs.
            using (var client = new PowerBIClient(_tokenCredentials))
            {
                var features = client.AvailableFeatures.GetAvailableFeatures();
                var a = client.BaseUri;  // returns "https://api.powerbi.com/"
                var c = await client.Capacities.GetCapacitiesAsync();  // returns collection of one, with name "mapbidev"
                var d = client.Datasets.GetDatasets();  // returns none
                var e = await client.Gateways.GetGatewaysAsync();  // returns none
                // These empty returns only happen when no Group ID is provided.
                // Is there any meaning to datasets/gateways without a group id?

                // group stuff
                ODataResponseListGroup filteredGroups = client.Groups.GetGroups("contains(name,'MAP Dev')");
                ODataResponseListGroup allGroups = await client.Groups.GetGroupsAsync();
                //ODataResponseListGroup allGroups = client.Groups.GetGroups();  // This works too
                Group lyncCanBeUsed = allGroups.Value.SingleOrDefault(g => g.Name == "MAP Dev");
                ODataResponseListReport allReports = client.Reports.GetReports();  // returns none

                // report stuff
                string oneReportId = default;
                string oneGroupId = default;
                foreach (var group in allGroups.Value)
                {
                    var datasets = client.Datasets.GetDatasets(group.Id);  // none
                    var reportsOfOneGroup = client.Reports.GetReports(group.Id);
                    if (group.Name == "MAP Dev")
                    {
                        oneReportId = reportsOfOneGroup.Value[0].Id;
                        oneGroupId = group.Id;
                    }
                }

                var reportByReportId = client.Reports.GetReport(oneReportId);
                var reportByGroupAndReportId =  client.Reports.GetReport(oneGroupId, oneReportId);

                // TODO: implement publishing
                // maybe use: client.Imports.PostImportWithFileInGroup();

                // TODO: implement embedded viewing
                // Read in detail: https://docs.microsoft.com/en-us/power-bi/developer/embed-sample-for-customers
                //     search for "Create the embed token"
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
    }
}


