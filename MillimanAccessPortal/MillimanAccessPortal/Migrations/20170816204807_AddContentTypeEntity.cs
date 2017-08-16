using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddContentTypeEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RootContentItem_Client_ClientId",
                table: "RootContentItem");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "RootContentItem");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "RootContentItem",
                newName: "ContentTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_RootContentItem_ClientId",
                table: "RootContentItem",
                newName: "IX_RootContentItem_ContentTypeId");

            migrationBuilder.AddColumn<List<long>>(
                name: "ClientIdList",
                table: "RootContentItem",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContentType",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    CanReduce = table.Column<bool>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentType", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_RootContentItem_ContentType_ContentTypeId",
                table: "RootContentItem",
                column: "ContentTypeId",
                principalTable: "ContentType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RootContentItem_ContentType_ContentTypeId",
                table: "RootContentItem");

            migrationBuilder.DropTable(
                name: "ContentType");

            migrationBuilder.DropColumn(
                name: "ClientIdList",
                table: "RootContentItem");

            migrationBuilder.RenameColumn(
                name: "ContentTypeId",
                table: "RootContentItem",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_RootContentItem_ContentTypeId",
                table: "RootContentItem",
                newName: "IX_RootContentItem_ClientId");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "RootContentItem",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RootContentItem_Client_ClientId",
                table: "RootContentItem",
                column: "ClientId",
                principalTable: "Client",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
