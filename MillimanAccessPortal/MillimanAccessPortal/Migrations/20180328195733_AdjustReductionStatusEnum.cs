using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class AdjustReductionStatusEnum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "ReductionStatus",
                table: "ContentReductionTask",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long));

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ContentReductionTask",
                type: "xid",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ContentPublicationRequest",
                type: "xid",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ContentPublicationRequest");

            migrationBuilder.AlterColumn<long>(
                name: "ReductionStatus",
                table: "ContentReductionTask",
                nullable: false,
                oldClrType: typeof(long),
                oldDefaultValue: 0L);
        }
    }
}
