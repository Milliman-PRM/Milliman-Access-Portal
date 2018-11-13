/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace MapDbContextLib.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
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
        public DbSet<FileUpload> FileUpload { get; set; }

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

            builder.HasPostgresExtension("uuid-ossp");  // enable server extension to support uuid generation functions

            builder.Entity<ApplicationUser>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.HasIndex("NormalizedEmail").IsUnique();
            });
            builder.Entity<ApplicationRole>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });

            builder.Entity<Client>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<UserRoleInClient>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<UserRoleInProfitCenter>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<UserRoleInRootContentItem>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<UserInSelectionGroup>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<SelectionGroup>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<RootContentItem>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<HierarchyField>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.Property(x => x.StructureType).HasDefaultValue(FieldStructureType.Unknown);
            });
            builder.Entity<HierarchyFieldValue>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<ContentType>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<ProfitCenter>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<ContentReductionTask>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.Property(x => x.ReductionStatus).HasDefaultValue(ReductionStatusEnum.Unspecified);
                b.HasOne(x => x.ContentPublicationRequest).WithMany().OnDelete(DeleteBehavior.Cascade);
                b.ForNpgsqlUseXminAsConcurrencyToken();
            });
            builder.Entity<ContentPublicationRequest>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.ForNpgsqlUseXminAsConcurrencyToken();
            });
            builder.Entity<FileUpload>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
        }

        public bool ClientExists(Guid id)
        {
            return Client.Any(e => e.Id == id);
        }

        private bool ProfitCenterExists(Guid id)
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
