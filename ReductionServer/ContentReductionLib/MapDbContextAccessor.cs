using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace ContentReductionLib
{
    public class MapDbContextAccessor
    {
        private DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder;

        public ApplicationDbContext Context { get { return new ApplicationDbContext(ContextBuilder.Options); } }

        public MapDbContextAccessor(string CxnStringName = "DefaultConnection")
        {
            ConfigurationBuilder CfgBuilder = new ConfigurationBuilder();
            CfgBuilder.AddUserSecrets<MapDbContextAccessor>();

            IConfigurationRoot MyConfig = CfgBuilder.Build();

            string CxStr = MyConfig.GetConnectionString(CxnStringName);
            ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            ContextBuilder.UseNpgsql(CxStr);
        }

    }
}
