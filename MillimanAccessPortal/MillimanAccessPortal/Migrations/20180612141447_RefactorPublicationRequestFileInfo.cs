using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class RefactorPublicationRequestFileInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResultingContentFiles",
                table: "ContentPublicationRequest",
                newName: "ReductionRelatedFiles");

            migrationBuilder.RenameColumn(
                name: "ContentRelatedFiles",
                table: "ContentPublicationRequest",
                newName: "LiveReadyFiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReductionRelatedFiles",
                table: "ContentPublicationRequest",
                newName: "ResultingContentFiles");

            migrationBuilder.RenameColumn(
                name: "LiveReadyFiles",
                table: "ContentPublicationRequest",
                newName: "ContentRelatedFiles");
        }
    }
}
