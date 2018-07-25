using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace AuditLogLib.Migrations
{
    public partial class RemoveSummary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Summary",
                table: "AuditEvent");

            migrationBuilder.RenameColumn(
                name: "TimeStamp",
                table: "AuditEvent",
                newName: "TimeStampUtc");

            migrationBuilder.RenameColumn(
                name: "EventDetailJsonb",
                table: "AuditEvent",
                newName: "EventData");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeStampUtc",
                table: "AuditEvent",
                newName: "TimeStamp");

            migrationBuilder.RenameColumn(
                name: "EventData",
                table: "AuditEvent",
                newName: "EventDetailJsonb");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "AuditEvent",
                nullable: true);
        }
    }
}
