using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace ContentReductionLib
{
    public class UserSecretsTest
    {
        IConfigurationRoot MyConfig = null;

        public UserSecretsTest()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddUserSecrets<UserSecretsTest>();
            MyConfig = builder.Build();
        }

        public string GetCxnString(string CsnStringName)
        {
            return MyConfig.GetConnectionString(CsnStringName);
        }

        public string GetCfgString(string CfgName)
        {
            return MyConfig[CfgName];
        }
    }
}
