using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class DateTimeFieldsToUtc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "ContentPublicationRequest");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreateDateTimeUtc",
                table: "ContentReductionTask",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

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
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "CreateDateTimeUtc",
                table: "ContentPublicationRequest");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreateDateTime",
                table: "ContentReductionTask",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreateDateTime",
                table: "ContentPublicationRequest",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");
        }
    }
}
