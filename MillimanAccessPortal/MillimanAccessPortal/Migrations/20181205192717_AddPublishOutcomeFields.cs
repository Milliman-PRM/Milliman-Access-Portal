using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddPublishOutcomeFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OutcomeMetadata",
                table: "ContentReductionTask",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutcomeMetadata",
                table: "ContentPublicationRequest",
                type: "jsonb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutcomeMetadata",
                table: "ContentReductionTask");

            migrationBuilder.DropColumn(
                name: "OutcomeMetadata",
                table: "ContentPublicationRequest");
        }
    }
}
