using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AuditLogLib.Migrations
{
    public partial class AddSummary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SourceApplication",
                table: "AuditEvent",
                newName: "Summary");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "AuditEvent",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "AuditEvent");

            migrationBuilder.RenameColumn(
                name: "Summary",
                table: "AuditEvent",
                newName: "SourceApplication");
        }
    }
}
