using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddClientAccessReview : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ClientAccessReview>(
                name: "LastAccessReview",
                table: "Client",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "jsonb_build_object('UserName', 'N/A', 'LastReviewDateTimeUtc', now() at time zone 'utc')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAccessReview",
                table: "Client");
        }
    }
}
