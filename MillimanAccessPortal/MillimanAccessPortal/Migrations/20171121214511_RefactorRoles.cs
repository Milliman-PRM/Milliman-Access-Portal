using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class RefactorRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleForContentItemUserGroup_ContentItemUserGroup_ContentItemUserGroupId",
                table: "UserRoleForContentItemUserGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleForContentItemUserGroup_AspNetRoles_RoleId",
                table: "UserRoleForContentItemUserGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleForContentItemUserGroup_AspNetUsers_UserId",
                table: "UserRoleForContentItemUserGroup");

            migrationBuilder.DropTable(
                name: "UserRoleForClient");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoleForContentItemUserGroup",
                table: "UserRoleForContentItemUserGroup");

            migrationBuilder.DropIndex(
                name: "IX_UserRoleForContentItemUserGroup_RoleId",
                table: "UserRoleForContentItemUserGroup");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "UserRoleForContentItemUserGroup");

            migrationBuilder.RenameTable(
                name: "UserRoleForContentItemUserGroup",
                newName: "UserInContentItemUserGroup");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoleForContentItemUserGroup_UserId",
                table: "UserInContentItemUserGroup",
                newName: "IX_UserInContentItemUserGroup_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoleForContentItemUserGroup_ContentItemUserGroupId",
                table: "UserInContentItemUserGroup",
                newName: "IX_UserInContentItemUserGroup_ContentItemUserGroupId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserInContentItemUserGroup",
                table: "UserInContentItemUserGroup",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "UserRoleInClient",
                columns: table => new
                {
                    Id = table.Column<long>(type: "int8", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ClientId = table.Column<long>(type: "int8", nullable: false),
                    RoleId = table.Column<long>(type: "int8", nullable: false),
                    UserId = table.Column<long>(type: "int8", nullable: false)
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
                name: "UserRoleInProfitCenter",
                columns: table => new
                {
                    Id = table.Column<long>(type: "int8", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ProfitCenterId = table.Column<long>(type: "int8", nullable: false),
                    RoleId = table.Column<long>(type: "int8", nullable: false),
                    UserId = table.Column<long>(type: "int8", nullable: false)
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
                name: "UserRoleInRootContentItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "int8", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    RoleId = table.Column<long>(type: "int8", nullable: false),
                    RootContentItemId = table.Column<long>(type: "int8", nullable: false),
                    UserId = table.Column<long>(type: "int8", nullable: false)
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

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserInContentItemUserGroup_ContentItemUserGroup_ContentItemUserGroupId",
                table: "UserInContentItemUserGroup",
                column: "ContentItemUserGroupId",
                principalTable: "ContentItemUserGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserInContentItemUserGroup_AspNetUsers_UserId",
                table: "UserInContentItemUserGroup",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_UserInContentItemUserGroup_ContentItemUserGroup_ContentItemUserGroupId",
                table: "UserInContentItemUserGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_UserInContentItemUserGroup_AspNetUsers_UserId",
                table: "UserInContentItemUserGroup");

            migrationBuilder.DropTable(
                name: "UserRoleInClient");

            migrationBuilder.DropTable(
                name: "UserRoleInProfitCenter");

            migrationBuilder.DropTable(
                name: "UserRoleInRootContentItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserInContentItemUserGroup",
                table: "UserInContentItemUserGroup");

            migrationBuilder.RenameTable(
                name: "UserInContentItemUserGroup",
                newName: "UserRoleForContentItemUserGroup");

            migrationBuilder.RenameIndex(
                name: "IX_UserInContentItemUserGroup_UserId",
                table: "UserRoleForContentItemUserGroup",
                newName: "IX_UserRoleForContentItemUserGroup_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserInContentItemUserGroup_ContentItemUserGroupId",
                table: "UserRoleForContentItemUserGroup",
                newName: "IX_UserRoleForContentItemUserGroup_ContentItemUserGroupId");

            migrationBuilder.AddColumn<long>(
                name: "RoleId",
                table: "UserRoleForContentItemUserGroup",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoleForContentItemUserGroup",
                table: "UserRoleForContentItemUserGroup",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "UserRoleForClient",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ClientId = table.Column<long>(nullable: false),
                    RoleId = table.Column<long>(nullable: false),
                    UserId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleForClient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleForClient_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleForClient_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleForClient_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleForContentItemUserGroup_RoleId",
                table: "UserRoleForContentItemUserGroup",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleForClient_ClientId",
                table: "UserRoleForClient",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleForClient_RoleId",
                table: "UserRoleForClient",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleForClient_UserId",
                table: "UserRoleForClient",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleForContentItemUserGroup_ContentItemUserGroup_ContentItemUserGroupId",
                table: "UserRoleForContentItemUserGroup",
                column: "ContentItemUserGroupId",
                principalTable: "ContentItemUserGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleForContentItemUserGroup_AspNetRoles_RoleId",
                table: "UserRoleForContentItemUserGroup",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleForContentItemUserGroup_AspNetUsers_UserId",
                table: "UserRoleForContentItemUserGroup",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
