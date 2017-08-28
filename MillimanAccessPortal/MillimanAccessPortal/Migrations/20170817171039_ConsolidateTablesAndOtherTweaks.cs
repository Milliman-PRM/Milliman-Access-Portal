using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MillimanAccessPortal.Migrations
{
    public partial class ConsolidateTablesAndOtherTweaks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HierarchyFieldValue_HierarchyFieldValue_ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue");

            migrationBuilder.DropTable(
                name: "ContentInstance");

            migrationBuilder.AlterColumn<string>(
                name: "ContentName",
                table: "RootContentItem",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<List<long>>(
                name: "ClientIdList",
                table: "RootContentItem",
                nullable: false,
                oldClrType: typeof(List<long>),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeSpecificDetail",
                table: "RootContentItem",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "HierarchyFieldValue",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<List<string>>(
                name: "FieldNameList",
                table: "HierarchyField",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ContentType",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<List<long>>(
                name: "SelectedHierarchyFieldValueList",
                table: "ContentItemUserGroup",
                nullable: false,
                oldClrType: typeof(List<long>),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GroupName",
                table: "ContentItemUserGroup",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentInstanceUrl",
                table: "ContentItemUserGroup",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Client",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<List<string>>(
                name: "AcceptedEmailDomainList",
                table: "Client",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HierarchyFieldValue_HierarchyFieldValue_ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue",
                column: "ParentHierarchyFieldValueId",
                principalTable: "HierarchyFieldValue",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HierarchyFieldValue_HierarchyFieldValue_ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue");

            migrationBuilder.DropColumn(
                name: "TypeSpecificDetail",
                table: "RootContentItem");

            migrationBuilder.DropColumn(
                name: "ContentInstanceUrl",
                table: "ContentItemUserGroup");

            migrationBuilder.AlterColumn<string>(
                name: "ContentName",
                table: "RootContentItem",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<List<long>>(
                name: "ClientIdList",
                table: "RootContentItem",
                nullable: true,
                oldClrType: typeof(List<long>));

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "HierarchyFieldValue",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<long>(
                name: "ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AlterColumn<List<string>>(
                name: "FieldNameList",
                table: "HierarchyField",
                nullable: true,
                oldClrType: typeof(List<string>));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ContentType",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<List<long>>(
                name: "SelectedHierarchyFieldValueList",
                table: "ContentItemUserGroup",
                nullable: true,
                oldClrType: typeof(List<long>));

            migrationBuilder.AlterColumn<string>(
                name: "GroupName",
                table: "ContentItemUserGroup",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Client",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<List<string>>(
                name: "AcceptedEmailDomainList",
                table: "Client",
                nullable: true,
                oldClrType: typeof(List<string>));

            migrationBuilder.CreateTable(
                name: "ContentInstance",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ContentItemUserGroupId = table.Column<long>(nullable: false),
                    RootContentItemId = table.Column<long>(nullable: false),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentInstance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentInstance_ContentItemUserGroup_ContentItemUserGroupId",
                        column: x => x.ContentItemUserGroupId,
                        principalTable: "ContentItemUserGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentInstance_RootContentItem_RootContentItemId",
                        column: x => x.RootContentItemId,
                        principalTable: "RootContentItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentInstance_ContentItemUserGroupId",
                table: "ContentInstance",
                column: "ContentItemUserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentInstance_RootContentItemId",
                table: "ContentInstance",
                column: "RootContentItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_HierarchyFieldValue_HierarchyFieldValue_ParentHierarchyFieldValueId",
                table: "HierarchyFieldValue",
                column: "ParentHierarchyFieldValueId",
                principalTable: "HierarchyFieldValue",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
