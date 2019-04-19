using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MapCommonLib;
using Newtonsoft.Json;
using PowerBILib;

namespace SamplePowerBILib
{

    class Program
    {

        static async Task Main(string[] args)
        {
            #region Setup

            string env_UserProfile = Environment.GetEnvironmentVariable("USERPROFILE");
            if (string.IsNullOrWhiteSpace(env_UserProfile))
            {
                throw new ApplicationException("No USERPROFILE environment Variable");
            }

            string configPath = Path.Combine(env_UserProfile, @"Desktop\pbiConfig.json");
            string pbixPath = Path.Combine(env_UserProfile, @"Desktop\more_pokemon.pbix");
            
            PowerBIConfig config = JsonConvert.DeserializeObject<PowerBIConfig>(File.ReadAllText(configPath));

            var client = new PowerBILibApi(config);

            bool gotToken = await client.GetAccessTokenAsync();

            if (gotToken)
            {
                Console.WriteLine($"Token type: {client.authToken.token_type}");
                Console.WriteLine($"Token value: {client.authToken.access_token}");
            }
            else
            {
                Console.WriteLine("Failed to get token");
                return;
            }
            #endregion

            #region Sample API calls
            List<PowerBIWorkspace> workspaces = await client.GetWorkspacesAsync();

            List<PowerBIReport> reports = await client.GetReportsInWorkspaceAsync(workspaces[0].id);

            PowerBIReport oneReport = await client.GetReportByIdAsync(reports[0].id);

            PowerBIReport publishedReport = await client.PublishPbixAsync(pbixPath, workspaces[0].id);
            #endregion
            // Always keep a breakpoint here to examine output before exiting
            return;
        }


    }
}
