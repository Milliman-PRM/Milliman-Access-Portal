using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddClientConfigOverride : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ClientConfigurationOverride>(
                name: "ConfigurationOverride",
                table: "Client",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "jsonb_build_object()");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfigurationOverride",
                table: "Client");
        }
    }
}
