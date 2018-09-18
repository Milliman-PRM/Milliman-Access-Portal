using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddReducedFileChecksumField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReducedContentChecksum",
                table: "SelectionGroup",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReducedContentChecksum",
                table: "SelectionGroup");
        }
    }
}
