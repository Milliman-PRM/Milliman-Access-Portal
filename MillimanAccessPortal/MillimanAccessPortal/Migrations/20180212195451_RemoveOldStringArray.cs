using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class RemoveOldStringArray : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FieldNameList",
                table: "HierarchyField");

            migrationBuilder.AlterColumn<string>(
                name: "FieldName",
                table: "HierarchyField",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FieldName",
                table: "HierarchyField",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<string[]>(
                name: "FieldNameList",
                table: "HierarchyField",
                nullable: true);
        }
    }
}
