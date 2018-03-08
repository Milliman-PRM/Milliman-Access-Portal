using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;

namespace MapDbContextLib.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, long>
    {
        public DbSet<Client> Client { get; set; }
        public DbSet<UserRoleInClient> UserRoleInClient { get; set; }
        public DbSet<UserRoleInProfitCenter> UserRoleInProfitCenter { get; set; }
        public DbSet<UserRoleInRootContentItem> UserRoleInRootContentItem { get; set; }
        public DbSet<UserInSelectionGroup> UserInSelectionGroup { get; set; }
        public DbSet<SelectionGroup> SelectionGroup { get; set; }
        public DbSet<RootContentItem> RootContentItem { get; set; }
        public DbSet<HierarchyField> HierarchyField { get; set; }
        public DbSet<HierarchyFieldValue> HierarchyFieldValue { get; set; }
        public DbSet<ContentType> ContentType { get; set; }
        public DbSet<ProfitCenter> ProfitCenter { get; set; }
        public DbSet<ContentReductionTask> ContentReductionTask { get; set; }
        public DbSet<ContentPublicationRequest> ContentPublicationRequest { get; set; }

        // Alteration of Identity entities
        public DbSet<ApplicationUser> ApplicationUser { get; set; }
        public DbSet<ApplicationRole> ApplicationRole { get; set; }

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

            builder.Entity<ContentPublicationRequest>()
                .Property(b => b.CreateDateTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Entity<ContentPublicationRequest>()
                .ForNpgsqlUseXminAsConcurrencyToken();

            builder.Entity<ContentReductionTask>()
                .Property(b => b.CreateDateTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Entity<ContentReductionTask>()
                .Property(b => b.Id)
                .HasDefaultValueSql("uuid_generate_v4()");

            builder.Entity<ContentReductionTask>()
                .Property(b => b.ReductionStatus)
                .HasDefaultValue(ReductionStatusEnum.Default);

            builder.Entity<ContentReductionTask>()
                .ForNpgsqlUseXminAsConcurrencyToken();

            builder.Entity<HierarchyField>()
                .Property(b => b.StructureType)
                .HasDefaultValue(FieldStructureType.Unknown);
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
            Identity.ApplicationRole.SeedRoles(serviceProvider).Wait();
            Context.ContentType.InitializeContentTypes(serviceProvider);
        }
    }
}
