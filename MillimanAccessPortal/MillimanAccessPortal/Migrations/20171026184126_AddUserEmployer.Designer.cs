using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20171026184126_AddUserEmployer")]
    partial class AddUserEmployer
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "1.1.2");

            modelBuilder.Entity("MapDbContextLib.Context.Client", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string[]>("AcceptedEmailAddressExceptionList");

                    b.Property<string[]>("AcceptedEmailDomainList")
                        .IsRequired();

                    b.Property<string>("ClientCode");

                    b.Property<string>("ConsultantEmail");

                    b.Property<string>("ConsultantName");

                    b.Property<string>("ConsultantOffice");

                    b.Property<string>("ContactEmail");

                    b.Property<string>("ContactName");

                    b.Property<string>("ContactPhone");

                    b.Property<string>("ContactTitle");

                    b.Property<string>("Name")
                        .IsRequired();

                    b.Property<long?>("ParentClientId");

                    b.Property<long>("ProfitCenterId");

                    b.HasKey("Id");

                    b.HasIndex("ParentClientId");

                    b.HasIndex("ProfitCenterId");

                    b.ToTable("Client");
                });

            modelBuilder.Entity("MapDbContextLib.Context.ContentItemUserGroup", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("ClientId");

                    b.Property<string>("ContentInstanceUrl")
                        .IsRequired();

                    b.Property<string>("GroupName")
                        .IsRequired();

                    b.Property<long>("RootContentItemId");

                    b.Property<long[]>("SelectedHierarchyFieldValueList")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("ClientId");

                    b.HasIndex("RootContentItemId");

                    b.ToTable("ContentItemUserGroup");
                });

            modelBuilder.Entity("MapDbContextLib.Context.ContentType", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("CanReduce");

                    b.Property<string>("Name")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("ContentType");
                });

            modelBuilder.Entity("MapDbContextLib.Context.HierarchyField", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string[]>("FieldNameList")
                        .IsRequired();

                    b.Property<int>("HierarchyLevel");

                    b.Property<long>("RootContentItemId");

                    b.HasKey("Id");

                    b.HasIndex("RootContentItemId");

                    b.ToTable("HierarchyField");
                });

            modelBuilder.Entity("MapDbContextLib.Context.HierarchyFieldValue", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("HierarchyLevel");

                    b.Property<long?>("ParentHierarchyFieldValueId");

                    b.Property<long>("RootContentItemId");

                    b.Property<string>("Value")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("ParentHierarchyFieldValueId");

                    b.HasIndex("RootContentItemId");

                    b.ToTable("HierarchyFieldValue");
                });

            modelBuilder.Entity("MapDbContextLib.Context.ProfitCenter", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ContactEmail");

                    b.Property<string>("ContactName");

                    b.Property<string>("ContactPhone");

                    b.Property<string>("ContactTitle");

                    b.Property<string>("MillimanOffice");

                    b.Property<string>("Name")
                        .IsRequired();

                    b.Property<string>("ProfitCenterCode");

                    b.HasKey("Id");

                    b.ToTable("ProfitCenter");
                });

            modelBuilder.Entity("MapDbContextLib.Context.RootContentItem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long[]>("ClientIdList")
                        .IsRequired();

                    b.Property<string>("ContentName")
                        .IsRequired();

                    b.Property<long>("ContentTypeId");

                    b.Property<string>("TypeSpecificDetail")
                        .HasColumnType("jsonb");

                    b.HasKey("Id");

                    b.HasIndex("ContentTypeId");

                    b.ToTable("RootContentItem");
                });

            modelBuilder.Entity("MapDbContextLib.Context.UserAuthorizationToClient", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("ClientId");

                    b.Property<long>("RoleId");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ClientId");

                    b.HasIndex("RoleId");

                    b.HasIndex("UserId");

                    b.ToTable("UserRoleForClient");
                });

            modelBuilder.Entity("MapDbContextLib.Context.UserInContentItemUserGroup", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("ContentItemUserGroupId");

                    b.Property<long>("RoleId");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ContentItemUserGroupId");

                    b.HasIndex("RoleId");

                    b.HasIndex("UserId");

                    b.ToTable("UserRoleForContentItemUserGroup");
                });

            modelBuilder.Entity("MapDbContextLib.Identity.ApplicationRole", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Name")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256);

                    b.Property<int>("RoleEnum");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("MapDbContextLib.Identity.ApplicationUser", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AccessFailedCount");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

                    b.Property<string>("Employer");

                    b.Property<string>("FirstName");

                    b.Property<string>("LastName");

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<string>("SecurityStamp");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<string>("UserName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRoleClaim<long>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<long>("RoleId");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserClaim<long>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserLogin<long>", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<long>("UserId");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserRole<long>", b =>
                {
                    b.Property<long>("UserId");

                    b.Property<long>("RoleId");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserToken<long>", b =>
                {
                    b.Property<long>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("MapDbContextLib.Context.Client", b =>
                {
                    b.HasOne("MapDbContextLib.Context.Client", "ParentClient")
                        .WithMany()
                        .HasForeignKey("ParentClientId");

                    b.HasOne("MapDbContextLib.Context.ProfitCenter", "ProfitCenter")
                        .WithMany()
                        .HasForeignKey("ProfitCenterId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MapDbContextLib.Context.ContentItemUserGroup", b =>
                {
                    b.HasOne("MapDbContextLib.Context.Client", "Client")
                        .WithMany()
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MapDbContextLib.Context.RootContentItem", "RootContentItem")
                        .WithMany()
                        .HasForeignKey("RootContentItemId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MapDbContextLib.Context.HierarchyField", b =>
                {
                    b.HasOne("MapDbContextLib.Context.RootContentItem", "RootContentItem")
                        .WithMany()
                        .HasForeignKey("RootContentItemId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MapDbContextLib.Context.HierarchyFieldValue", b =>
                {
                    b.HasOne("MapDbContextLib.Context.HierarchyFieldValue", "ParentValue")
                        .WithMany()
                        .HasForeignKey("ParentHierarchyFieldValueId");

                    b.HasOne("MapDbContextLib.Context.RootContentItem", "RootContentItem")
                        .WithMany()
                        .HasForeignKey("RootContentItemId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MapDbContextLib.Context.RootContentItem", b =>
                {
                    b.HasOne("MapDbContextLib.Context.ContentType", "ContentType")
                        .WithMany()
                        .HasForeignKey("ContentTypeId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MapDbContextLib.Context.UserAuthorizationToClient", b =>
                {
                    b.HasOne("MapDbContextLib.Context.Client", "Client")
                        .WithMany()
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MapDbContextLib.Identity.ApplicationRole", "Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MapDbContextLib.Identity.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MapDbContextLib.Context.UserInContentItemUserGroup", b =>
                {
                    b.HasOne("MapDbContextLib.Context.ContentItemUserGroup", "ContentItemUserGroup")
                        .WithMany()
                        .HasForeignKey("ContentItemUserGroupId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MapDbContextLib.Identity.ApplicationRole", "Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MapDbContextLib.Identity.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRoleClaim<long>", b =>
                {
                    b.HasOne("MapDbContextLib.Identity.ApplicationRole")
                        .WithMany("Claims")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserClaim<long>", b =>
                {
                    b.HasOne("MapDbContextLib.Identity.ApplicationUser")
                        .WithMany("Claims")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserLogin<long>", b =>
                {
                    b.HasOne("MapDbContextLib.Identity.ApplicationUser")
                        .WithMany("Logins")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserRole<long>", b =>
                {
                    b.HasOne("MapDbContextLib.Identity.ApplicationRole")
                        .WithMany("Users")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MapDbContextLib.Identity.ApplicationUser")
                        .WithMany("Roles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
