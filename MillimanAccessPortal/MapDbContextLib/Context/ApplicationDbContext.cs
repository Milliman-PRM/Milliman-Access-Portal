/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: The Entity Framework context class for the MAP application database
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

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
        public DbSet<AuthenticationScheme> AuthenticationScheme { get; set; }
        public DbSet<NameValueConfiguration> NameValueConfiguration { get; set; }

        public DbSet<SftpAccount> SftpAccount { get; set; }
        public DbSet<SftpConnection> SftpConnection { get; set; }
        public DbSet<FileDrop> FileDrop { get; set; }
        public DbSet<FileDropUserPermissionGroup> FileDropUserPermissionGroup { get; set; }
        public DbSet<FileDropDirectory> FileDropDirectory { get; set; }
        public DbSet<FileDropFile> FileDropFile { get; set; }

        // Alteration of Identity entities
        public DbSet<ApplicationUser> ApplicationUser { get; set; }
        public DbSet<ApplicationRole> ApplicationRole { get; set; }

        // Had to implement this parameterless constructor for Mocking in unit tests, I hope this doesn't cause any problem in EF
        public ApplicationDbContext() { }

        static ApplicationDbContext()
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<AuthenticationType>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<PublicationStatus>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<ReductionStatusEnum>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<ContentTypeEnum>();
        }
            

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {}

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            builder.ForNpgsqlHasEnum<AuthenticationType>();
            builder.ForNpgsqlHasEnum<PublicationStatus>();
            builder.ForNpgsqlHasEnum<ReductionStatusEnum>();
            builder.ForNpgsqlHasEnum<ContentTypeEnum>();

            builder.HasPostgresExtension("uuid-ossp");  // enable server extension to support uuid generation functions
            builder.HasPostgresExtension("citext");  // enable server extension to support case insensitive text field type

            builder.Entity<ApplicationUser>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.HasIndex(x => x.NormalizedEmail).IsUnique();
                b.Property(x => x.IsUserAgreementAccepted).HasDefaultValue(null);
            });
            builder.Entity<ApplicationRole>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });

            builder.Entity<AuthenticationScheme>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.HasAlternateKey(s => s.Name);
            });
            builder.Entity<Client>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.Property(x => x.DomainListCountLimit).HasDefaultValue(GlobalFunctions.DefaultClientDomainListCountLimit);
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
                b.Property(x => x.FileExtensions).HasDefaultValueSql("'{}'");
            });
            builder.Entity<ProfitCenter>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<ContentReductionTask>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.Property(x => x.ProcessingStartDateTimeUtc).HasDefaultValue(new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Utc));
                b.Property(x => x.ReductionStatus).HasDefaultValue(ReductionStatusEnum.Unspecified);
                b.HasOne(x => x.ContentPublicationRequest).WithMany().OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.SelectionGroup).WithMany().OnDelete(DeleteBehavior.Cascade);
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
            builder.Entity<NameValueConfiguration>(b =>
            {
                b.Property(x => x.Value).HasDefaultValue("");
            });

            builder.Entity<FileDrop>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.HasIndex(x => x.RootPath).IsUnique();
            });
            builder.Entity<FileDropUserPermissionGroup>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<SftpAccount>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.HasOne(x => x.ApplicationUser).WithMany().OnDelete(DeleteBehavior.Cascade);  // not the default when a nullable FK
                b.Property(x => x.PasswordResetDateTimeUtc).HasDefaultValue(DateTime.MinValue).ValueGeneratedOnAdd();
            });
            builder.Entity<SftpConnection>(b =>
            {
                b.Property(x => x.Id).IsRequired().ValueGeneratedNever();
                b.Property(x => x.CreatedDateTimeUtc).HasDefaultValueSql("(now() at time zone 'utc')").ValueGeneratedOnAdd();
                b.Property(x => x.LastActivityUtc).HasDefaultValueSql("(now() at time zone 'utc')").ValueGeneratedOnAdd();
            });
            builder.Entity<FileDropDirectory>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.HasOne(x => x.ParentDirectoryEntry).WithMany(p => p.ChildDirectories).OnDelete(DeleteBehavior.Cascade);  // not the default when a nullable FK
                b.HasOne(x => x.CreatedByAccount).WithMany(p => p.Directories).OnDelete(DeleteBehavior.Cascade);  // not the default when a nullable FK
            });
            builder.Entity<FileDropFile>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.HasOne(x => x.Directory).WithMany(p => p.Files).OnDelete(DeleteBehavior.Restrict);  // not the default when a nullable FK
                b.HasOne(x => x.CreatedByAccount).WithMany(p => p.Files).OnDelete(DeleteBehavior.Restrict);  // not the default when a nullable FK
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

        public static async Task InitializeAll(IServiceProvider serviceProvider)
        {
            await Identity.ApplicationRole.SeedRoles(serviceProvider);
            Context.ContentType.InitializeContentTypes(serviceProvider);
            await Context.AuthenticationScheme.SeedSchemes(serviceProvider);
            Context.NameValueConfiguration.InitializeNameValueConfiguration(serviceProvider);
        }
    }

    public class IdPropertyComparer<T> : IEqualityComparer<T> where T: class 
    {
        public bool Equals(T l, T r)
        {
            if (l.GetType() != r.GetType()) return false;
            if (ReferenceEquals(l, r)) return true;
            if (l is null || r is null) return false;

            Type t = typeof(T);
            PropertyInfo propInfo = t.GetProperty("Id");

            var lValue = propInfo.GetValue(l);
            var rValue = propInfo.GetValue(r);

            return lValue.Equals(rValue);
        }

        public int GetHashCode(T obj)
        {
            PropertyInfo p = typeof(T).GetProperty("Id");
            var idVal = p.GetValue(obj);
            return idVal.GetHashCode();
        }
    }
}
