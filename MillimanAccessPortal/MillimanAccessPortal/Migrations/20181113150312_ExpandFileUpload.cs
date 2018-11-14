using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class ExpandFileUpload : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StoragePath",
                table: "FileUpload",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "Checksum",
                table: "FileUpload",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<DateTime>(
                name: "InitiatedDateTimeUtc",
                table: "FileUpload",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "FileUpload",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StatusMessage",
                table: "FileUpload",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitiatedDateTimeUtc",
                table: "FileUpload");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FileUpload");

            migrationBuilder.DropColumn(
                name: "StatusMessage",
                table: "FileUpload");

            migrationBuilder.AlterColumn<string>(
                name: "StoragePath",
                table: "FileUpload",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Checksum",
                table: "FileUpload",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
