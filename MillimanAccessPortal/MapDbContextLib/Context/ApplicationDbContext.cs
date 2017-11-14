using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Identity;

namespace MapDbContextLib.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, long>
    {
        public virtual DbSet<Client> Client { get; set; }
        public virtual DbSet<UserAuthorizationToClient> UserRoleForClient { get; set; }
        public virtual DbSet<UserInContentItemUserGroup> UserRoleForContentItemUserGroup { get; set; }
        public virtual DbSet<ContentItemUserGroup> ContentItemUserGroup { get; set; }
        public virtual DbSet<RootContentItem> RootContentItem { get; set; }
        public virtual DbSet<HierarchyField> HierarchyField { get; set; }
        public virtual DbSet<HierarchyFieldValue> HierarchyFieldValue { get; set; }
        public virtual DbSet<ContentType> ContentType { get; set; }
        public virtual DbSet<ProfitCenter> ProfitCenter { get; set; }

        // Alteration of Identity entities
        public virtual DbSet<ApplicationUser> ApplicationUser { get; set; }
        public virtual DbSet<ApplicationRole> ApplicationRole { get; set; }

        // Had to implement this parameterless constructor for Mocking in unit tests, I hope this doesn't cause any problem in EF
        public ApplicationDbContext() { }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {}

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        public bool ClientExists(long id)
        {
            return Client.Any(e => e.Id == id);
        }

        private bool ProfitCenterExists(long id)
        {
            return ProfitCenter.Any(pc => pc.Id == id);
        }

        public static void InitializeAll(IServiceProvider serviceProvider)
        {
            Identity.ApplicationRole.SeedRoles(serviceProvider);
            Context.ContentType.InitializeContentTypes(serviceProvider);
        }
    }
}
