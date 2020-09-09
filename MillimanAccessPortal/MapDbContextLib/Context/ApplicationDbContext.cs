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
        public virtual DbSet<Client> Client { get; set; }
        public virtual DbSet<UserRoleInClient> UserRoleInClient { get; set; }
        public virtual DbSet<UserRoleInProfitCenter> UserRoleInProfitCenter { get; set; }
        public virtual DbSet<UserRoleInRootContentItem> UserRoleInRootContentItem { get; set; }
        public virtual DbSet<UserInSelectionGroup> UserInSelectionGroup { get; set; }
        public virtual DbSet<SelectionGroup> SelectionGroup { get; set; }
        public virtual DbSet<RootContentItem> RootContentItem { get; set; }
        public virtual DbSet<HierarchyField> HierarchyField { get; set; }
        public virtual DbSet<HierarchyFieldValue> HierarchyFieldValue { get; set; }
        public virtual DbSet<ContentType> ContentType { get; set; }
        public virtual DbSet<ProfitCenter> ProfitCenter { get; set; }
        public virtual DbSet<ContentReductionTask> ContentReductionTask { get; set; }
        public virtual DbSet<ContentPublicationRequest> ContentPublicationRequest { get; set; }
        public virtual DbSet<FileUpload> FileUpload { get; set; }
        public virtual DbSet<AuthenticationScheme> AuthenticationScheme { get; set; }
        public virtual DbSet<NameValueConfiguration> NameValueConfiguration { get; set; }

        public virtual DbSet<SftpAccount> SftpAccount { get; set; }
        public virtual DbSet<FileDrop> FileDrop { get; set; }
        public virtual DbSet<FileDropUserPermissionGroup> FileDropUserPermissionGroup { get; set; }
        public virtual DbSet<FileDropDirectory> FileDropDirectory { get; set; }
        public virtual DbSet<FileDropFile> FileDropFile { get; set; }

        // Alteration of Identity entities
        public virtual DbSet<ApplicationUser> ApplicationUser { get; set; }
        public virtual DbSet<ApplicationRole> ApplicationRole { get; set; }

        // Had to implement this parameterless constructor for Mocking in unit tests, I hope this doesn't cause any problem in EF
        public ApplicationDbContext() { }

        static ApplicationDbContext()
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<AuthenticationType>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<PublicationStatus>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<ReductionStatusEnum>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<ContentTypeEnum>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<FileDropNotificationType>();
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

            builder.HasPostgresEnum<AuthenticationType>();
            builder.HasPostgresEnum<PublicationStatus>();
            builder.HasPostgresEnum<ReductionStatusEnum>();
            builder.HasPostgresEnum<ContentTypeEnum>();
            builder.HasPostgresEnum<FileDropNotificationType>();

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
                b.Property(x => x.LastAccessReview).HasDefaultValueSql("jsonb_build_object('UserName', 'N/A', 'LastReviewDateTimeUtc', now() at time zone 'utc')");
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
                b.UseXminAsConcurrencyToken();
            });
            builder.Entity<ContentPublicationRequest>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.UseXminAsConcurrencyToken();
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
                b.HasIndex(x => x.ShortHash).IsUnique();
            });
            builder.Entity<FileDropUserPermissionGroup>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
            });
            builder.Entity<SftpAccount>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.HasOne(x => x.ApplicationUser).WithMany(u => u.SftpAccounts).OnDelete(DeleteBehavior.Cascade);  // not the default when a nullable FK
                b.Property(x => x.PasswordResetDateTimeUtc).HasDefaultValue(DateTime.MinValue).ValueGeneratedOnAdd();
                b.HasOne(x => x.FileDrop).WithMany(fd => fd.SftpAccounts).OnDelete(DeleteBehavior.Cascade);  // not the default when a nullable FK
                b.HasOne(x => x.FileDropUserPermissionGroup).WithMany(g => g.SftpAccounts).OnDelete(DeleteBehavior.SetNull);  // Keep account to sustain credentials
            });
            builder.Entity<FileDropDirectory>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.HasOne(x => x.ParentDirectory).WithMany(p => p.ChildDirectories).OnDelete(DeleteBehavior.Cascade);  // not the default when a nullable FK
                b.HasIndex(x => new { x.FileDropId, x.CanonicalFileDropPath }).IsUnique();
            });
            builder.Entity<FileDropFile>(b =>
            {
                b.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()").ValueGeneratedOnAdd();
                b.HasOne(x => x.Directory).WithMany(p => p.Files).OnDelete(DeleteBehavior.Restrict);  // not the default when a nullable FK
                b.HasOne(x => x.CreatedByAccount).WithMany(p => p.Files).OnDelete(DeleteBehavior.Restrict);  // not the default when a nullable FK
                b.HasIndex(x => new { x.DirectoryId, x.FileName }).IsUnique();  // unique because sftp clients can make multiple requests to create the same item
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string cxnstr = optionsBuilder.Options.GetExtension<Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal.NpgsqlOptionsExtension>().ConnectionString;
            optionsBuilder.UseNpgsql(cxnstr, o => o.SetPostgresVersion(9, 6));
        }

        public static async Task InitializeAllAsync(IServiceProvider serviceProvider)
        {
            await Identity.ApplicationRole.SeedRolesAsync(serviceProvider);
            await Context.ContentType.InitializeContentTypesAsync(serviceProvider);
            await Context.AuthenticationScheme.SeedSchemesAsync(serviceProvider);
            await Context.NameValueConfiguration.InitializeNameValueConfigurationAsync(serviceProvider);
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
