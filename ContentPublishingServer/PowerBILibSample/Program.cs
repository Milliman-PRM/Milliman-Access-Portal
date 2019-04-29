using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MapCommonLib;
using Newtonsoft.Json;
using PowerBiLib;

namespace SamplePowerBiLib
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
            
            PowerBiConfig config = JsonConvert.DeserializeObject<PowerBiConfig>(File.ReadAllText(configPath));
            #endregion

            PowerBiLibApi lib = await new PowerBiLibApi(config).InitializeAsync();
            await lib.Demonstrate(pbixPath);

        }
    }
}
