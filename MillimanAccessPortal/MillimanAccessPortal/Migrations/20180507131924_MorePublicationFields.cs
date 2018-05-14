using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class MorePublicationFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MasterFilePath",
                table: "ContentPublicationRequest");

            migrationBuilder.AddColumn<bool>(
                name: "IsMaster",
                table: "SelectionGroup",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DoesReduce",
                table: "RootContentItem",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContentRelatedFiles",
                table: "ContentPublicationRequest",
                type: "jsonb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMaster",
                table: "SelectionGroup");

            migrationBuilder.DropColumn(
                name: "DoesReduce",
                table: "RootContentItem");

            migrationBuilder.DropColumn(
                name: "ContentRelatedFiles",
                table: "ContentPublicationRequest");

            migrationBuilder.AddColumn<string>(
                name: "MasterFilePath",
                table: "ContentPublicationRequest",
                nullable: false,
                defaultValue: "");
        }
    }
}
