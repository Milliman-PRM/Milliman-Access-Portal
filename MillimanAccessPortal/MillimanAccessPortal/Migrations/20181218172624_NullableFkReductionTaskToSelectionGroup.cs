using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class NullableFkReductionTaskToSelectionGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SelectionGroupId",
                table: "ContentReductionTask",
                nullable: true,
                oldClrType: typeof(Guid));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SelectionGroupId",
                table: "ContentReductionTask",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);
        }
    }
}
