using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AuditLogLib
{
    internal class AuditLogDbContext : DbContext
    {
        []
        internal static AuditLogDbContext Instance
        {
            get
            {
                // TODO get the connection string from configuration
                var builder = new DbContextOptionsBuilder<AuditLogDbContext>();
                builder.UseNpgsql("Server=127.0.0.1;Database=MapAuditLog;User Id=postgres;Password=postgres;");
                return new AuditLogDbContext(builder.Options);
            }
        }

        protected AuditLogDbContext(DbContextOptions<AuditLogDbContext> options)
            : base(options)
        {}

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
