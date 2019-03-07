using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddContentDisclaimer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DisclaimerAccepted",
                table: "UserInSelectionGroup",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContentDisclaimer",
                table: "RootContentItem",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisclaimerAccepted",
                table: "UserInSelectionGroup");

            migrationBuilder.DropColumn(
                name: "ContentDisclaimer",
                table: "RootContentItem");
        }
    }
}
