using System;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace ContentReductionLib
{
    public class MapDbContextAccessor
    {
        private static object ContextBuilderLock = new object();
        private static DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder = null;

        public static ApplicationDbContext New {
            get
            {
                lock(ContextBuilderLock)
                {
                    if (ContextBuilder == null)
                    {
                        throw new NullReferenceException("Attempting to construct new ApplicationDbContext but connection string not initialized");
                    }
                    return new ApplicationDbContext(ContextBuilder.Options);
                }
            }
        }

        public static bool UseConfiguredConnectionString(string CfgParamName = "DefaultConnection")
        {
            try
            {
                ConfigurationBuilder CfgBuilder = new ConfigurationBuilder();
                CfgBuilder.AddUserSecrets<MapDbContextAccessor>()
                          .AddJsonFile("appsettings.json", true);
                IConfigurationRoot MyConfig = CfgBuilder.Build();

                string CxStr = MyConfig.GetConnectionString(CfgParamName);
                return UseConnectionString(CxStr);
            }
            catch
            {
                return false;
            }
        }

        public static bool UseConnectionString(string CxnString)
        {
            try
            {
                lock(ContextBuilderLock)
                {
                    ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    ContextBuilder.UseNpgsql(CxnString);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
