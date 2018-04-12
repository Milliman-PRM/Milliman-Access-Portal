using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddUserMessageField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReductionStatusMessage",
                table: "ContentReductionTask",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReductionStatusMessage",
                table: "ContentReductionTask");
        }
    }
}
