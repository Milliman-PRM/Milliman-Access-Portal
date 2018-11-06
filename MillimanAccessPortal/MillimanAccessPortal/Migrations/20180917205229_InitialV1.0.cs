using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class InitialV10 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", "'uuid-ossp', '', ''");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
                    RoleEnum = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    AccessFailedCount = table.Column<int>(nullable: false),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    Employer = table.Column<string>(nullable: true),
                    FirstName = table.Column<string>(nullable: true),
                    IsSuspended = table.Column<bool>(nullable: false),
                    LastName = table.Column<string>(nullable: true),
                    LastPasswordChangeDateTimeUtc = table.Column<DateTime>(nullable: false),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                    PasswordHash = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    PreviousPasswords = table.Column<string>(type: "jsonb", nullable: true),
                    SecurityStamp = table.Column<string>(nullable: true),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    UserName = table.Column<string>(maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentType",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CanReduce = table.Column<bool>(nullable: false),
                    DefaultIconName = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileUpload",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Checksum = table.Column<string>(nullable: false),
                    ClientFileIdentifier = table.Column<string>(nullable: false),
                    CreatedDateTimeUtc = table.Column<DateTime>(nullable: false),
                    StoragePath = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileUpload", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfitCenter",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ContactEmail = table.Column<string>(nullable: true),
                    ContactName = table.Column<string>(nullable: true),
                    ContactPhone = table.Column<string>(nullable: true),
                    ContactTitle = table.Column<string>(nullable: true),
                    MillimanOffice = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: false),
                    ProfitCenterCode = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitCenter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true),
                    RoleId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true),
                    UserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    ProviderKey = table.Column<string>(nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false),
                    RoleId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false),
                    LoginProvider = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Client",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    AcceptedEmailAddressExceptionList = table.Column<string[]>(nullable: false),
                    AcceptedEmailDomainList = table.Column<string[]>(nullable: false),
                    ClientCode = table.Column<string>(nullable: true),
                    ConsultantEmail = table.Column<string>(nullable: true),
                    ConsultantName = table.Column<string>(nullable: true),
                    ConsultantOffice = table.Column<string>(nullable: true),
                    ContactEmail = table.Column<string>(nullable: true),
                    ContactName = table.Column<string>(nullable: true),
                    ContactPhone = table.Column<string>(nullable: true),
                    ContactTitle = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: false),
                    NewUserWelcomeText = table.Column<string>(nullable: true),
                    ParentClientId = table.Column<Guid>(nullable: true),
                    ProfitCenterId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Client", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Client_Client_ParentClientId",
                        column: x => x.ParentClientId,
                        principalTable: "Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Client_ProfitCenter_ProfitCenterId",
                        column: x => x.ProfitCenterId,
                        principalTable: "ProfitCenter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleInProfitCenter",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ProfitCenterId = table.Column<Guid>(nullable: false),
                    RoleId = table.Column<Guid>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleInProfitCenter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleInProfitCenter_ProfitCenter_ProfitCenterId",
                        column: x => x.ProfitCenterId,
                        principalTable: "ProfitCenter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleInProfitCenter_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleInProfitCenter_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RootContentItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ClientId = table.Column<Guid>(nullable: false),
                    ContentFiles = table.Column<string>(type: "jsonb", nullable: true),
                    ContentName = table.Column<string>(nullable: false),
                    ContentTypeId = table.Column<Guid>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    DoesReduce = table.Column<bool>(nullable: false),
                    IsSuspended = table.Column<bool>(nullable: false),
                    Notes = table.Column<string>(nullable: true),
                    TypeSpecificDetail = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RootContentItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RootContentItem_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RootContentItem_ContentType_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleInClient",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ClientId = table.Column<Guid>(nullable: false),
                    RoleId = table.Column<Guid>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleInClient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleInClient_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleInClient_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleInClient_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentPublicationRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ApplicationUserId = table.Column<Guid>(nullable: false),
                    CreateDateTimeUtc = table.Column<DateTime>(nullable: false),
                    LiveReadyFiles = table.Column<string>(type: "jsonb", nullable: true),
                    ReductionRelatedFiles = table.Column<string>(type: "jsonb", nullable: true),
                    RequestStatus = table.Column<int>(nullable: false),
                    ResultHierarchy = table.Column<string>(type: "jsonb", nullable: true),
                    RootContentItemId = table.Column<Guid>(nullable: false),
                    StatusMessage = table.Column<string>(nullable: true),
                    UploadedRelatedFiles = table.Column<string>(type: "jsonb", nullable: true),
                    xmin = table.Column<uint>(type: "xid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPublicationRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentPublicationRequest_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentPublicationRequest_RootContentItem_RootContentItemId",
                        column: x => x.RootContentItemId,
                        principalTable: "RootContentItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HierarchyField",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FieldDelimiter = table.Column<string>(nullable: true),
                    FieldDisplayName = table.Column<string>(nullable: false),
                    FieldName = table.Column<string>(nullable: false),
                    RootContentItemId = table.Column<Guid>(nullable: false),
                    StructureType = table.Column<int>(nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarchyField", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HierarchyField_RootContentItem_RootContentItemId",
                        column: x => x.RootContentItemId,
                        principalTable: "RootContentItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SelectionGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ContentInstanceUrl = table.Column<string>(nullable: true),
                    GroupName = table.Column<string>(nullable: false),
                    IsMaster = table.Column<bool>(nullable: false),
                    IsSuspended = table.Column<bool>(nullable: false),
                    ReducedContentChecksum = table.Column<string>(nullable: true),
                    RootContentItemId = table.Column<Guid>(nullable: false),
                    SelectedHierarchyFieldValueList = table.Column<Guid[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectionGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelectionGroup_RootContentItem_RootContentItemId",
                        column: x => x.RootContentItemId,
                        principalTable: "RootContentItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleInRootContentItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    RoleId = table.Column<Guid>(nullable: false),
                    RootContentItemId = table.Column<Guid>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleInRootContentItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleInRootContentItem_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleInRootContentItem_RootContentItem_RootContentItemId",
                        column: x => x.RootContentItemId,
                        principalTable: "RootContentItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleInRootContentItem_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HierarchyFieldValue",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    HierarchyFieldId = table.Column<Guid>(nullable: false),
                    Value = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarchyFieldValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HierarchyFieldValue_HierarchyField_HierarchyFieldId",
                        column: x => x.HierarchyFieldId,
                        principalTable: "HierarchyField",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentReductionTask",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ApplicationUserId = table.Column<Guid>(nullable: false),
                    ContentPublicationRequestId = table.Column<Guid>(nullable: true),
                    CreateDateTimeUtc = table.Column<DateTime>(nullable: false),
                    MasterContentChecksum = table.Column<string>(nullable: true),
                    MasterContentHierarchy = table.Column<string>(type: "jsonb", nullable: true),
                    MasterFilePath = table.Column<string>(nullable: false),
                    ReducedContentChecksum = table.Column<string>(nullable: true),
                    ReducedContentHierarchy = table.Column<string>(type: "jsonb", nullable: true),
                    ReductionStatus = table.Column<long>(nullable: false, defaultValue: 0L),
                    ReductionStatusMessage = table.Column<string>(nullable: true),
                    ResultFilePath = table.Column<string>(nullable: true),
                    SelectionCriteria = table.Column<string>(type: "jsonb", nullable: true),
                    SelectionGroupId = table.Column<Guid>(nullable: false),
                    TaskAction = table.Column<int>(nullable: false),
                    xmin = table.Column<uint>(type: "xid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentReductionTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentReductionTask_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPublicationRequestId",
                        column: x => x.ContentPublicationRequestId,
                        principalTable: "ContentPublicationRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentReductionTask_SelectionGroup_SelectionGroupId",
                        column: x => x.SelectionGroupId,
                        principalTable: "SelectionGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInSelectionGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    SelectionGroupId = table.Column<Guid>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInSelectionGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInSelectionGroup_SelectionGroup_SelectionGroupId",
                        column: x => x.SelectionGroupId,
                        principalTable: "SelectionGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserInSelectionGroup_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Client_ParentClientId",
                table: "Client",
                column: "ParentClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Client_ProfitCenterId",
                table: "Client",
                column: "ProfitCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPublicationRequest_ApplicationUserId",
                table: "ContentPublicationRequest",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPublicationRequest_RootContentItemId",
                table: "ContentPublicationRequest",
                column: "RootContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReductionTask_ApplicationUserId",
                table: "ContentReductionTask",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReductionTask_ContentPublicationRequestId",
                table: "ContentReductionTask",
                column: "ContentPublicationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReductionTask_SelectionGroupId",
                table: "ContentReductionTask",
                column: "SelectionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarchyField_RootContentItemId",
                table: "HierarchyField",
                column: "RootContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarchyFieldValue_HierarchyFieldId",
                table: "HierarchyFieldValue",
                column: "HierarchyFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_RootContentItem_ClientId",
                table: "RootContentItem",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_RootContentItem_ContentTypeId",
                table: "RootContentItem",
                column: "ContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SelectionGroup_RootContentItemId",
                table: "SelectionGroup",
                column: "RootContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInSelectionGroup_SelectionGroupId",
                table: "UserInSelectionGroup",
                column: "SelectionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInSelectionGroup_UserId",
                table: "UserInSelectionGroup",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleInClient_ClientId",
                table: "UserRoleInClient",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleInClient_RoleId",
                table: "UserRoleInClient",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleInClient_UserId",
                table: "UserRoleInClient",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleInProfitCenter_ProfitCenterId",
                table: "UserRoleInProfitCenter",
                column: "ProfitCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleInProfitCenter_RoleId",
                table: "UserRoleInProfitCenter",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleInProfitCenter_UserId",
                table: "UserRoleInProfitCenter",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleInRootContentItem_RoleId",
                table: "UserRoleInRootContentItem",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleInRootContentItem_RootContentItemId",
                table: "UserRoleInRootContentItem",
                column: "RootContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleInRootContentItem_UserId",
                table: "UserRoleInRootContentItem",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ContentReductionTask");

            migrationBuilder.DropTable(
                name: "FileUpload");

            migrationBuilder.DropTable(
                name: "HierarchyFieldValue");

            migrationBuilder.DropTable(
                name: "UserInSelectionGroup");

            migrationBuilder.DropTable(
                name: "UserRoleInClient");

            migrationBuilder.DropTable(
                name: "UserRoleInProfitCenter");

            migrationBuilder.DropTable(
                name: "UserRoleInRootContentItem");

            migrationBuilder.DropTable(
                name: "ContentPublicationRequest");

            migrationBuilder.DropTable(
                name: "HierarchyField");

            migrationBuilder.DropTable(
                name: "SelectionGroup");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "RootContentItem");

            migrationBuilder.DropTable(
                name: "Client");

            migrationBuilder.DropTable(
                name: "ContentType");

            migrationBuilder.DropTable(
                name: "ProfitCenter");
        }
    }
}
