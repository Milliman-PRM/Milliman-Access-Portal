using System;
using System.IO;
using System.Threading.Tasks;
using MapCommonLib;
using Newtonsoft.Json;
using PowerBILib;

namespace SamplePowerBILib
{

    class Program
    {

        static void Main(string[] args)
        {
            #region Setup

            // Substitute the path to your config file and PBIX file below
            string configPath = @"C:\Users\ben.wyatt\Desktop\pbiConfig.json";
            string pbixPath = @"C:\users\ben.wyatt\desktop\more_pokemon.pbix";
            
            PowerBIConfig config = JsonConvert.DeserializeObject<PowerBIConfig>(File.ReadAllText(configPath));

            var client = new PowerBILibApi(config);

            bool gotToken = client.GetAccessTokenAsync().Result;

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
            PowerBIWorkspace[] workspaces = client.GetWorkspaces();

            PowerBIReport[] reports = client.GetReportsInWorkspace(workspaces[0].id);

            PowerBIReport oneReport = client.GetReportById(reports[0].id);

            PowerBIReport publishedReport = client.PublishPbix(pbixPath, workspaces[0].id);
            #endregion
            // Always keep a breakpoint here to examine output before exiting
            return;
        }


    }
}
