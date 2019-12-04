using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddReductionWarningStatusValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TYPE reduction_status_enum ADD VALUE IF NOT EXISTS 'warning' AFTER 'replaced'", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
