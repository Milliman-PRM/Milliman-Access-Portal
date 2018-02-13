using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class RemoveClientIdFromSelectionGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SelectionGroup_Client_ClientId",
                table: "SelectionGroup");

            migrationBuilder.DropIndex(
                name: "IX_SelectionGroup_ClientId",
                table: "SelectionGroup");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "SelectionGroup");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ClientId",
                table: "SelectionGroup",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_SelectionGroup_ClientId",
                table: "SelectionGroup",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_SelectionGroup_Client_ClientId",
                table: "SelectionGroup",
                column: "ClientId",
                principalTable: "Client",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
