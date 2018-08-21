using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class UserAndContentIsSuspendedFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "RootContentItem",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "RootContentItem");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "AspNetUsers");
        }
    }
}
