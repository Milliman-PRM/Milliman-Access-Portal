using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace AuditLogLib.Migrations
{
    public partial class InitialV10 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvent",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Assembly = table.Column<string>(nullable: true),
                    EventData = table.Column<string>(type: "jsonb", nullable: true),
                    EventType = table.Column<string>(nullable: true),
                    SessionId = table.Column<string>(nullable: true),
                    Source = table.Column<string>(nullable: true),
                    TimeStampUtc = table.Column<DateTime>(nullable: false),
                    User = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvent", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvent");
        }
    }
}
