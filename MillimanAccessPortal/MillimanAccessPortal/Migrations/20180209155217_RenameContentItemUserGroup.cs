using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class RenameContentItemUserGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserInContentItemUserGroup");

            migrationBuilder.DropTable(
                name: "ContentItemUserGroup");

            migrationBuilder.CreateTable(
                name: "SelectionGroup",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ClientId = table.Column<long>(nullable: false),
                    ContentInstanceUrl = table.Column<string>(nullable: false),
                    GroupName = table.Column<string>(nullable: false),
                    RootContentItemId = table.Column<long>(nullable: false),
                    SelectedHierarchyFieldValueList = table.Column<long[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectionGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelectionGroup_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SelectionGroup_RootContentItem_RootContentItemId",
                        column: x => x.RootContentItemId,
                        principalTable: "RootContentItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInSelectionGroup",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    SelectionGroupId = table.Column<long>(nullable: false),
                    UserId = table.Column<long>(nullable: false)
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
                name: "IX_SelectionGroup_ClientId",
                table: "SelectionGroup",
                column: "ClientId");

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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserInSelectionGroup");

            migrationBuilder.DropTable(
                name: "SelectionGroup");

            migrationBuilder.CreateTable(
                name: "ContentItemUserGroup",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ClientId = table.Column<long>(nullable: false),
                    ContentInstanceUrl = table.Column<string>(nullable: false),
                    GroupName = table.Column<string>(nullable: false),
                    RootContentItemId = table.Column<long>(nullable: false),
                    SelectedHierarchyFieldValueList = table.Column<long[]>(nullable: false)
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
                name: "UserInContentItemUserGroup",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ContentItemUserGroupId = table.Column<long>(nullable: false),
                    UserId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInContentItemUserGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInContentItemUserGroup_ContentItemUserGroup_ContentItemUserGroupId",
                        column: x => x.ContentItemUserGroupId,
                        principalTable: "ContentItemUserGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserInContentItemUserGroup_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemUserGroup_ClientId",
                table: "ContentItemUserGroup",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemUserGroup_RootContentItemId",
                table: "ContentItemUserGroup",
                column: "RootContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInContentItemUserGroup_ContentItemUserGroupId",
                table: "UserInContentItemUserGroup",
                column: "ContentItemUserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInContentItemUserGroup_UserId",
                table: "UserInContentItemUserGroup",
                column: "UserId");
        }
    }
}
