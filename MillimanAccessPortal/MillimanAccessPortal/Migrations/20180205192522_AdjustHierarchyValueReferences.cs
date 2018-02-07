using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class AdjustHierarchyValueReferences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HierarchyFieldValue_HierarchyFieldValue_ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue");

            migrationBuilder.DropForeignKey(
                name: "FK_HierarchyFieldValue_RootContentItem_RootContentItemId",
                table: "HierarchyFieldValue");

            migrationBuilder.DropIndex(
                name: "IX_HierarchyFieldValue_ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue");

            migrationBuilder.DropColumn(
                name: "HierarchyLevel",
                table: "HierarchyFieldValue");

            migrationBuilder.DropColumn(
                name: "ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue");

            migrationBuilder.RenameColumn(
                name: "RootContentItemId",
                table: "HierarchyFieldValue",
                newName: "HierarchyFieldId");

            migrationBuilder.RenameIndex(
                name: "IX_HierarchyFieldValue_RootContentItemId",
                table: "HierarchyFieldValue",
                newName: "IX_HierarchyFieldValue_HierarchyFieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_HierarchyFieldValue_HierarchyField_HierarchyFieldId",
                table: "HierarchyFieldValue",
                column: "HierarchyFieldId",
                principalTable: "HierarchyField",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HierarchyFieldValue_HierarchyField_HierarchyFieldId",
                table: "HierarchyFieldValue");

            migrationBuilder.RenameColumn(
                name: "HierarchyFieldId",
                table: "HierarchyFieldValue",
                newName: "RootContentItemId");

            migrationBuilder.RenameIndex(
                name: "IX_HierarchyFieldValue_HierarchyFieldId",
                table: "HierarchyFieldValue",
                newName: "IX_HierarchyFieldValue_RootContentItemId");

            migrationBuilder.AddColumn<int>(
                name: "HierarchyLevel",
                table: "HierarchyFieldValue",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HierarchyFieldValue_ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue",
                column: "ParentHierarchyFieldValueId");

            migrationBuilder.AddForeignKey(
                name: "FK_HierarchyFieldValue_HierarchyFieldValue_ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue",
                column: "ParentHierarchyFieldValueId",
                principalTable: "HierarchyFieldValue",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HierarchyFieldValue_RootContentItem_RootContentItemId",
                table: "HierarchyFieldValue",
                column: "RootContentItemId",
                principalTable: "RootContentItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
