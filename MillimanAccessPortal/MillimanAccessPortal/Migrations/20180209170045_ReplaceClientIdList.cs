using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class ReplaceClientIdList : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientIdList",
                table: "RootContentItem");

            migrationBuilder.AddColumn<long>(
                name: "ClientId",
                table: "RootContentItem",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_RootContentItem_ClientId",
                table: "RootContentItem",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_RootContentItem_Client_ClientId",
                table: "RootContentItem",
                column: "ClientId",
                principalTable: "Client",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RootContentItem_Client_ClientId",
                table: "RootContentItem");

            migrationBuilder.DropIndex(
                name: "IX_RootContentItem_ClientId",
                table: "RootContentItem");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "RootContentItem");

            migrationBuilder.AddColumn<long[]>(
                name: "ClientIdList",
                table: "RootContentItem",
                nullable: true);
        }
    }
}
