using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class PublicationRequestUploadedRelatedFiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadedRelatedFiles",
                table: "ContentPublicationRequest",
                type: "jsonb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadedRelatedFiles",
                table: "ContentPublicationRequest");
        }
    }
}
