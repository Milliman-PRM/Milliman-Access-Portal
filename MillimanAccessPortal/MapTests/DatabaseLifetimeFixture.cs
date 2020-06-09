/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Implements a collection fixture as discussed in https://xunit.net/docs/shared-context
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MapTests
{
    [CollectionDefinition("DatabaseLifetime collection")]
    public class DatabaseLifeTimeCollection : ICollectionFixture<DatabaseLifetimeFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }


    public class DatabaseLifetimeFixture : IDisposable
    {
        public string ConnectionString { get; private set; }
        public IConfiguration Config { get; private set; }

        public DatabaseLifetimeFixture()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Debug()
                .CreateLogger();

            #region Get configuration and set instance properties
            Config = TestInitialization.GenerateConfiguration();

            Dictionary<string,string> DbConfig = new Dictionary<string, string>
            {
                { "DbHost", Config.GetValue<string>("UnitTestPgServerHost") },
                { "DbUser", Config.GetValue<string>("UnitTestPgServerUser") },
                { "DbPass", Config.GetValue<string>("UnitTestPgServerPass") },
                { "DbPort", Config.GetValue<string>("UnitTestPgServerPort") },
            };

            if (DbConfig.Any(v => string.IsNullOrWhiteSpace(v.Value)))
            {
                throw new ApplicationException("Database configuration is incomplete or not found");
            }

            Npgsql.NpgsqlConnectionStringBuilder cxnStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Database = Guid.NewGuid().ToString(),
                Port = int.Parse(DbConfig["DbPort"]),
                Host = DbConfig["DbHost"],
                Username = DbConfig["DbUser"],
                Password = DbConfig["DbPass"],
                SslMode = Npgsql.SslMode.Prefer,
            };

            ConnectionString = cxnStringBuilder.ConnectionString;
            #endregion

            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            builder.UseNpgsql(ConnectionString, o => o.SetPostgresVersion(9, 6));
            using (ApplicationDbContext db = new ApplicationDbContext(builder.Options))
            {
                db.Database.EnsureCreated();
            }
        }

        public void Dispose()
        {
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            builder.UseNpgsql(ConnectionString);
            using (ApplicationDbContext db = new ApplicationDbContext(builder.Options))
            {
                db.Database.EnsureDeleted();
            }
        }
    }
}
