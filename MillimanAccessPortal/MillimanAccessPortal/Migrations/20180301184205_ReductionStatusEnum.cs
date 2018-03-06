using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class ReductionStatusEnum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "ContentReductionTask");

            migrationBuilder.AddColumn<long>(
                name: "ReductionStatus",
                table: "ContentReductionTask",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReductionStatus",
                table: "ContentReductionTask");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ContentReductionTask",
                nullable: false,
                defaultValue: "");
        }
    }
}
