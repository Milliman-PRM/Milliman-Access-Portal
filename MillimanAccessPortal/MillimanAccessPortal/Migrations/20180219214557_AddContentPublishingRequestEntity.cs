using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddContentPublishingRequestEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "ResultHierarchy",
                table: "ContentReductionTask");

            migrationBuilder.RenameColumn(
                name: "ResultReducedContentFile",
                table: "ContentReductionTask",
                newName: "ResultContentFile");

            migrationBuilder.AddColumn<long>(
                name: "ContentPublicationRequestId",
                table: "ContentReductionTask",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SelectionGroupId",
                table: "ContentReductionTask",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "ContentPublicationRequest",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ApplicationUserId = table.Column<long>(nullable: false),
                    CreateDateTime = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ResultHierarchy = table.Column<string>(type: "jsonb", nullable: true),
                    RootContentItemId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPublicationRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentPublicationRequest_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentPublicationRequest_RootContentItem_RootContentItemId",
                        column: x => x.RootContentItemId,
                        principalTable: "RootContentItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentReductionTask_ContentPublicationRequestId",
                table: "ContentReductionTask",
                column: "ContentPublicationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReductionTask_SelectionGroupId",
                table: "ContentReductionTask",
                column: "SelectionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPublicationRequest_ApplicationUserId",
                table: "ContentPublicationRequest",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPublicationRequest_RootContentItemId",
                table: "ContentPublicationRequest",
                column: "RootContentItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPublicationRequestId",
                table: "ContentReductionTask",
                column: "ContentPublicationRequestId",
                principalTable: "ContentPublicationRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReductionTask_SelectionGroup_SelectionGroupId",
                table: "ContentReductionTask",
                column: "SelectionGroupId",
                principalTable: "SelectionGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPublicationRequestId",
                table: "ContentReductionTask");

            migrationBuilder.DropForeignKey(
                name: "FK_ContentReductionTask_SelectionGroup_SelectionGroupId",
                table: "ContentReductionTask");

            migrationBuilder.DropTable(
                name: "ContentPublicationRequest");

            migrationBuilder.DropIndex(
                name: "IX_ContentReductionTask_ContentPublicationRequestId",
                table: "ContentReductionTask");

            migrationBuilder.DropIndex(
                name: "IX_ContentReductionTask_SelectionGroupId",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "ContentPublicationRequestId",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "SelectionGroupId",
                table: "ContentReductionTask");

            migrationBuilder.RenameColumn(
                name: "ResultContentFile",
                table: "ContentReductionTask",
                newName: "ResultReducedContentFile");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreateDateTime",
                table: "ContentReductionTask",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "ResultHierarchy",
                table: "ContentReductionTask",
                type: "jsonb",
                nullable: true);
        }
    }
}
