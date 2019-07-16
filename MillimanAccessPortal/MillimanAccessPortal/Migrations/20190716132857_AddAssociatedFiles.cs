using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddAssociatedFiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssociatedFiles",
                table: "RootContentItem",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LiveReadyAssociatedFiles",
                table: "ContentPublicationRequest",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedAssociatedFiles",
                table: "ContentPublicationRequest",
                type: "jsonb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssociatedFiles",
                table: "RootContentItem");

            migrationBuilder.DropColumn(
                name: "LiveReadyAssociatedFiles",
                table: "ContentPublicationRequest");

            migrationBuilder.DropColumn(
                name: "RequestedAssociatedFiles",
                table: "ContentPublicationRequest");
        }
    }
}
