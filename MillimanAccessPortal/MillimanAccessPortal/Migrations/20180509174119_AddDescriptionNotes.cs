using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddDescriptionNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RootContentItem",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "RootContentItem",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "RootContentItem");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "RootContentItem");
        }
    }
}
