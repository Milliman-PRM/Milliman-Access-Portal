using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddUserAgreement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUserAgreementAccepted",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NameValueConfiguration",
                columns: table => new
                {
                    Key = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameValueConfiguration", x => x.Key);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NameValueConfiguration");

            migrationBuilder.DropColumn(
                name: "IsUserAgreementAccepted",
                table: "AspNetUsers");
        }
    }
}
