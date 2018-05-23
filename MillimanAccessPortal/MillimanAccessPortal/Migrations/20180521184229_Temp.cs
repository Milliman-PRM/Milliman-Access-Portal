using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class Temp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "ContentPublicationRequest");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDateTimeUtc",
                table: "ContentPublicationRequest",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreateDateTimeUtc",
                table: "ContentPublicationRequest");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreateDateTime",
                table: "ContentPublicationRequest",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");
        }
    }
}
