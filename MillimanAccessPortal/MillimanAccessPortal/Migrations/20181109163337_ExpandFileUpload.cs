using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class ExpandFileUpload : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "FileUpload");

            migrationBuilder.DropColumn(
                name: "StoragePath",
                table: "FileUpload");

            migrationBuilder.RenameColumn(
                name: "CreatedDateTimeUtc",
                table: "FileUpload",
                newName: "InitiatedDateTimeUtc");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "FileUpload",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StatusMessage",
                table: "FileUpload",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FileUploadExtension",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Checksum = table.Column<string>(nullable: false),
                    CreatedDateTimeUtc = table.Column<DateTime>(nullable: false),
                    FileUploadId = table.Column<Guid>(nullable: false),
                    StoragePath = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileUploadExtension", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileUploadExtension_FileUpload_FileUploadId",
                        column: x => x.FileUploadId,
                        principalTable: "FileUpload",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileUploadExtension_FileUploadId",
                table: "FileUploadExtension",
                column: "FileUploadId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileUploadExtension");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FileUpload");

            migrationBuilder.DropColumn(
                name: "StatusMessage",
                table: "FileUpload");

            migrationBuilder.RenameColumn(
                name: "InitiatedDateTimeUtc",
                table: "FileUpload",
                newName: "CreatedDateTimeUtc");

            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "FileUpload",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoragePath",
                table: "FileUpload",
                nullable: false,
                defaultValue: "");
        }
    }
}
