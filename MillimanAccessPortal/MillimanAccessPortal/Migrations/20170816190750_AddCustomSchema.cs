using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddCustomSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RootContentItem",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ClientId = table.Column<long>(nullable: false),
                    ContentName = table.Column<string>(nullable: true),
                    ContentType = table.Column<string>(nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "ContentItemUserGroup",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ClientId = table.Column<long>(nullable: false),
                    GroupName = table.Column<string>(nullable: true),
                    RootContentItemId = table.Column<long>(nullable: false),
                    SelectedHierarchyFieldValueList = table.Column<List<long>>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItemUserGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentItemUserGroup_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentItemUserGroup_RootContentItem_RootContentItemId",
                        column: x => x.RootContentItemId,
                        principalTable: "RootContentItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HierarchyField",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    FieldNameList = table.Column<List<string>>(nullable: true),
                    HierarchyLevel = table.Column<int>(nullable: false),
                    RootContentItemId = table.Column<long>(nullable: false)
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
                name: "HierarchyFieldValue",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    HierarchyLevel = table.Column<int>(nullable: false),
                    ParentHierarchyFieldValueId = table.Column<long>(nullable: false),
                    RootContentItemId = table.Column<long>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarchyFieldValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HierarchyFieldValue_HierarchyFieldValue_ParentHierarchyFieldValueId",
                        column: x => x.ParentHierarchyFieldValueId,
                        principalTable: "HierarchyFieldValue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HierarchyFieldValue_RootContentItem_RootContentItemId",
                        column: x => x.RootContentItemId,
                        principalTable: "RootContentItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentInstance",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ContentItemUserGroupId = table.Column<long>(nullable: false),
                    RootContentItemId = table.Column<long>(nullable: false),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentInstance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentInstance_ContentItemUserGroup_ContentItemUserGroupId",
                        column: x => x.ContentItemUserGroupId,
                        principalTable: "ContentItemUserGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentInstance_RootContentItem_RootContentItemId",
                        column: x => x.RootContentItemId,
                        principalTable: "RootContentItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleForContentItemUserGroup",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ContentItemUserGroupId = table.Column<long>(nullable: false),
                    RoleId = table.Column<long>(nullable: false),
                    UserId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleForContentItemUserGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleForContentItemUserGroup_ContentItemUserGroup_ContentItemUserGroupId",
                        column: x => x.ContentItemUserGroupId,
                        principalTable: "ContentItemUserGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleForContentItemUserGroup_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleForContentItemUserGroup_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentInstance_ContentItemUserGroupId",
                table: "ContentInstance",
                column: "ContentItemUserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentInstance_RootContentItemId",
                table: "ContentInstance",
                column: "RootContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemUserGroup_ClientId",
                table: "ContentItemUserGroup",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemUserGroup_RootContentItemId",
                table: "ContentItemUserGroup",
                column: "RootContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarchyField_RootContentItemId",
                table: "HierarchyField",
                column: "RootContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarchyFieldValue_ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue",
                column: "ParentHierarchyFieldValueId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarchyFieldValue_RootContentItemId",
                table: "HierarchyFieldValue",
                column: "RootContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RootContentItem_ClientId",
                table: "RootContentItem",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleForContentItemUserGroup_ContentItemUserGroupId",
                table: "UserRoleForContentItemUserGroup",
                column: "ContentItemUserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleForContentItemUserGroup_RoleId",
                table: "UserRoleForContentItemUserGroup",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleForContentItemUserGroup_UserId",
                table: "UserRoleForContentItemUserGroup",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentInstance");

            migrationBuilder.DropTable(
                name: "HierarchyField");

            migrationBuilder.DropTable(
                name: "HierarchyFieldValue");

            migrationBuilder.DropTable(
                name: "UserRoleForContentItemUserGroup");

            migrationBuilder.DropTable(
                name: "ContentItemUserGroup");

            migrationBuilder.DropTable(
                name: "RootContentItem");
        }
    }
}
