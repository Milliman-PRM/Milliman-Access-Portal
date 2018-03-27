/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Npgsql;

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
                .HasDefaultValue(ReductionStatusEnum.Unspecified);

            builder.Entity<ContentReductionTask>()
                .ForNpgsqlUseXminAsConcurrencyToken();

            builder.Entity<HierarchyField>()
                .Property(b => b.StructureType)
                .HasDefaultValue(FieldStructureType.Unknown);
        }

        public ReductionStatusEnum GetPublicationRequestStatus(long RequestId)
        {
            using (var command = Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "WITH BaseTable AS ( " +
                                          "SELECT \"ContentPublicationRequestId\", ARRAY_AGG(DISTINCT \"ReductionStatus\")::bigint[] as status_list " +
                                          "FROM \"ContentReductionTask\" " +
                                          "WHERE \"ContentPublicationRequestId\" = @p0 " +
                                          "GROUP BY \"ContentPublicationRequestId\" ) " +
                                      "SELECT " +
                                          "CASE " +
                                          "WHEN 90::bigint = ANY(status_list) THEN 90::bigint " + // One or more tasks has the Error status
                                          "WHEN status_list<@ ARRAY[40::bigint, 3::bigint] THEN 40::bigint " +  // All tasks are live
                                          "WHEN array_length(status_list, 1) = 1 THEN status_list[1] " +  // All task statuses are the same; return whatever that status is
                                          "WHEN status_list <@ ARRAY[10::bigint, 20::bigint, 30::bigint] THEN 20::bigint " +
                                          "ELSE 0::bigint " +  // Default
                                          "END as \"PublicationRequestStatus\" " +
                                      "From BaseTable; ";

                var p0 = new NpgsqlParameter { ParameterName = "p0", DbType = DbType.Int64, Direction = ParameterDirection.Input, Value = RequestId };
                command.Parameters.Add(p0);

                Database.OpenConnection();
                using (DbDataReader result = command.ExecuteReader())
                {
                    if (result.Read() && result.HasRows && result.VisibleFieldCount == 1)
                    {
                        ReductionStatusEnum ReturnStatus = (ReductionStatusEnum)result.GetInt64(0);
                        return ReturnStatus;
                    }
                }
            }
            
            return ReductionStatusEnum.Unspecified;  // TODO maybe this should throw instead
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
