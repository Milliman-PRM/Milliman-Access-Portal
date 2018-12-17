using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class NullableFkReductionTaskToSelectionGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentReductionTask_SelectionGroup_SelectionGroupId",
                table: "ContentReductionTask");

            migrationBuilder.AlterColumn<Guid>(
                name: "SelectionGroupId",
                table: "ContentReductionTask",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReductionTask_SelectionGroup_SelectionGroupId",
                table: "ContentReductionTask",
                column: "SelectionGroupId",
                principalTable: "SelectionGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentReductionTask_SelectionGroup_SelectionGroupId",
                table: "ContentReductionTask");

            migrationBuilder.AlterColumn<Guid>(
                name: "SelectionGroupId",
                table: "ContentReductionTask",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReductionTask_SelectionGroup_SelectionGroupId",
                table: "ContentReductionTask",
                column: "SelectionGroupId",
                principalTable: "SelectionGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
