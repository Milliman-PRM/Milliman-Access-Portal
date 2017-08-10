using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuditLogLib
{
    internal class AuditLogDbContext : DbContext
    {
        [NotMapped]
        internal static string ConnectionString = string.Empty;

        [NotMapped]
        internal static AuditLogDbContext Instance
        {
            get
            {
                // TODO get the connection string from configuration
                if (ConnectionString == string.Empty)
                {
                    ConnectionString = "Server=127.0.0.1;Database=MapAuditLog;User Id=postgres;Password=postgres;";
                }

                var builder = new DbContextOptionsBuilder<AuditLogDbContext>();
                builder.UseNpgsql(ConnectionString);
                return new AuditLogDbContext(builder.Options);
            }
        }

        public DbSet<AuditEvent> AuditEvent { get; set; }

        public AuditLogDbContext()
            : base()
        {
            // This constructor works in combination with the OnConfiguring override to establish the connection
            // When te application uses this provider through when building a migration, or if the application instantiates with no argument. 
            // TODO do this better
            ConnectionString = "Server=127.0.0.1;Database=MapAuditLog;User Id=postgres;Password=postgres;";
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
            Builder.UseNpgsql(ConnectionString);
        }
    }
}
