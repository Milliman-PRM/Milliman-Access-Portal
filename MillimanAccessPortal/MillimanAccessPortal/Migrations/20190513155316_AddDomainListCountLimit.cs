using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddDomainListCountLimit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DomainListCountLimit",
                table: "Client",
                nullable: false,
                defaultValue: 3);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DomainListCountLimit",
                table: "Client");
        }
    }
}
