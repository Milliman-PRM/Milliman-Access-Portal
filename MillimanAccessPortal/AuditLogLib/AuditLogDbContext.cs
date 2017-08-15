using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.Configuration;

namespace AuditLogLib
{
    internal class AuditLogDbContext : DbContext
    {
        /// <summary>
        /// A convenience method allowing the user simplest access to an instance without the caller going through the contextbuilder for each instantiation
        /// </summary>
        /// <param name="ConnectionString">If not provided, a configured value (or default) is used</param>
        /// <returns></returns>
        internal static AuditLogDbContext Instance(string ConnectionString = "")
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                ConnectionString = GetConfiguredConnectionString();  // I could pass a connection string name to get a designated configured value
            }

            var builder = new DbContextOptionsBuilder<AuditLogDbContext>();
            builder.UseNpgsql(ConnectionString);
            return new AuditLogDbContext(builder.Options);
        }

        public DbSet<AuditEvent> AuditEvent { get; set; }

        public AuditLogDbContext()
            : base()
        {
            // This constructor works in combination with the OnConfiguring override to establish the connection
            // When the application uses this provider through when building a migration, or if the application instantiates with no argument. 
        }

        protected AuditLogDbContext(DbContextOptions<AuditLogDbContext> options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder Builder)
        {
            Microsoft.EntityFrameworkCore.Infrastructure.Internal.NpgsqlOptionsExtension Extension = 
                Builder.Options.Extensions.First(x => x.GetType() == typeof(Microsoft.EntityFrameworkCore.Infrastructure.Internal.NpgsqlOptionsExtension)) as Microsoft.EntityFrameworkCore.Infrastructure.Internal.NpgsqlOptionsExtension;
            //Builder.UseNpgsql(GetConfiguredConnectionString()); // Fix this
            Builder.UseNpgsql(Extension.ConnectionString); // Fix this
        }

        /// <summary>
        /// Has responsibility for extracting the proper ConfigurationString for this data context
        /// </summary>
        /// <param name="ConnectionStringName"></param>
        /// <returns></returns>
        internal static string GetConfiguredConnectionString(string ConnectionStringName = "AuditLogConnectionString")
        {
            // TODO Figure out how to get the connection string from configuration
            return "Server=127.0.0.1;Database=MapAuditLog;User Id=postgres;Password=postgres;";
        }

    }
}
