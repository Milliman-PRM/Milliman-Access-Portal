using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class NewFieldsInHierarchyField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HierarchyLevel",
                table: "HierarchyField");

            migrationBuilder.AlterColumn<string[]>(
                name: "FieldNameList",
                table: "HierarchyField",
                nullable: true,
                oldClrType: typeof(string[]));

            migrationBuilder.AddColumn<string>(
                name: "FieldDelimiter",
                table: "HierarchyField",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FieldDisplayName",
                table: "HierarchyField",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FieldName",
                table: "HierarchyField",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StructureType",
                table: "HierarchyField",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "ContentReductionTask",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldDefaultValueSql: "CURRENT_TIMESTAMP");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FieldDelimiter",
                table: "HierarchyField");

            migrationBuilder.DropColumn(
                name: "FieldDisplayName",
                table: "HierarchyField");

            migrationBuilder.DropColumn(
                name: "FieldName",
                table: "HierarchyField");

            migrationBuilder.DropColumn(
                name: "StructureType",
                table: "HierarchyField");

            migrationBuilder.AlterColumn<string[]>(
                name: "FieldNameList",
                table: "HierarchyField",
                nullable: false,
                oldClrType: typeof(string[]),
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HierarchyLevel",
                table: "HierarchyField",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "ContentReductionTask",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");
        }
    }
}
