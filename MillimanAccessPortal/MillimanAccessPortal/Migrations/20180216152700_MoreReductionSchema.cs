using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class MoreReductionSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ContentReductionTask",
                nullable: false,
                defaultValueSql: "uuid_generate_v4()",
                oldClrType: typeof(Guid));

            migrationBuilder.AddColumn<string>(
                name: "MasterContentFile",
                table: "ContentReductionTask",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultHierarchy",
                table: "ContentReductionTask",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultReducedContentFile",
                table: "ContentReductionTask",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectionCriteria",
                table: "ContentReductionTask",
                type: "jsonb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MasterContentFile",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "ResultHierarchy",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "ResultReducedContentFile",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "SelectionCriteria",
                table: "ContentReductionTask");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ContentReductionTask",
                nullable: false,
                oldClrType: typeof(Guid),
                oldDefaultValueSql: "uuid_generate_v4()");

            migrationBuilder.Sql("DROP EXTENSION IF EXISTS \"uuid-ossp\"");
        }
    }
}
