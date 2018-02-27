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
    public class UserSecretsTest
    {
        IConfigurationRoot MyConfig = null;
        ApplicationDbContext DbContext = null;

        public UserSecretsTest()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddUserSecrets<UserSecretsTest>();
            MyConfig = builder.Build();
        }

        public void UseCxnString(string CsnStringName)
        {
            string CxStr = MyConfig.GetConnectionString(CsnStringName);

            DbContextOptionsBuilder<ApplicationDbContext> builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            builder.UseNpgsql(CxStr);

            DbContext = new ApplicationDbContext(builder.Options);

            var x = DbContext.ContentType.Where(ct => ct.Id == 1);
        }

    }
}
