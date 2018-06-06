using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class NullableSelectedValueArray : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long[]>(
                name: "SelectedHierarchyFieldValueList",
                table: "SelectionGroup",
                nullable: true,
                oldClrType: typeof(long[]));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long[]>(
                name: "SelectedHierarchyFieldValueList",
                table: "SelectionGroup",
                nullable: false,
                oldClrType: typeof(long[]),
                oldNullable: true);
        }
    }
}
