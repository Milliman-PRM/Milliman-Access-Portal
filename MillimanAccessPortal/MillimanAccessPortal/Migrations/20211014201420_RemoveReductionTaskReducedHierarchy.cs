using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class RemoveReductionTaskReducedHierarchy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReducedContentHierarchy",
                table: "ContentReductionTask");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReducedContentHierarchy",
                table: "ContentReductionTask",
                type: "jsonb",
                nullable: true);
        }
    }
}
