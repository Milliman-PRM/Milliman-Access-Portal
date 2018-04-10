using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddReductionTaskFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExtractedHierarchy",
                table: "ContentReductionTask",
                newName: "ReducedContentHierarchy");

            migrationBuilder.AddColumn<string>(
                name: "MasterContentChecksum",
                table: "ContentReductionTask",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasterContentHierarchy",
                table: "ContentReductionTask",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReducedContentChecksum",
                table: "ContentReductionTask",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MasterContentChecksum",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "MasterContentHierarchy",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "ReducedContentChecksum",
                table: "ContentReductionTask");

            migrationBuilder.RenameColumn(
                name: "ReducedContentHierarchy",
                table: "ContentReductionTask",
                newName: "ExtractedHierarchy");
        }
    }
}
