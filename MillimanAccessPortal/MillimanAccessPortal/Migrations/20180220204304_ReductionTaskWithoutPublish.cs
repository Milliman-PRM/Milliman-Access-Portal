using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class ReductionTaskWithoutPublish : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPublicationRequestId",
                table: "ContentReductionTask");

            migrationBuilder.AlterColumn<long>(
                name: "ContentPublicationRequestId",
                table: "ContentReductionTask",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddColumn<long>(
                name: "ApplicationUserId",
                table: "ContentReductionTask",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_ContentReductionTask_ApplicationUserId",
                table: "ContentReductionTask",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReductionTask_AspNetUsers_ApplicationUserId",
                table: "ContentReductionTask",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPublicationRequestId",
                table: "ContentReductionTask",
                column: "ContentPublicationRequestId",
                principalTable: "ContentPublicationRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentReductionTask_AspNetUsers_ApplicationUserId",
                table: "ContentReductionTask");

            migrationBuilder.DropForeignKey(
                name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPublicationRequestId",
                table: "ContentReductionTask");

            migrationBuilder.DropIndex(
                name: "IX_ContentReductionTask_ApplicationUserId",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "ContentReductionTask");

            migrationBuilder.AlterColumn<long>(
                name: "ContentPublicationRequestId",
                table: "ContentReductionTask",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPublicationRequestId",
                table: "ContentReductionTask",
                column: "ContentPublicationRequestId",
                principalTable: "ContentPublicationRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
