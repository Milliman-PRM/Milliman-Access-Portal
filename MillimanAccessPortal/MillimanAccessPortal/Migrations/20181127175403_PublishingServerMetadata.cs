using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class PublishingServerMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string[]>(
                name: "FileExtensions",
                table: "ContentType",
                nullable: false,
                defaultValueSql: "'{}'",
                oldClrType: typeof(string[]));

            migrationBuilder.AddColumn<string>(
                name: "TaskMetadata",
                table: "ContentReductionTask",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestMetadata",
                table: "ContentPublicationRequest",
                type: "jsonb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskMetadata",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "RequestMetadata",
                table: "ContentPublicationRequest");

            migrationBuilder.AlterColumn<string[]>(
                name: "FileExtensions",
                table: "ContentType",
                nullable: false,
                oldClrType: typeof(string[]),
                oldDefaultValueSql: "'{}'");
        }
    }
}
