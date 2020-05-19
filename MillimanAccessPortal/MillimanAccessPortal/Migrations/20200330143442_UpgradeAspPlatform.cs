using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class UpgradeAspPlatform : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ContentTypeEnum>(
                name: "TypeEnum",
                table: "ContentType",
                nullable: false,
                defaultValue: ContentTypeEnum.Unknown);

            migrationBuilder.Sql(
                "UPDATE \"ContentType\" " +
                "SET \"TypeEnum\" = CASE " +
                "WHEN \"Name\" ILIKE 'Qlikview' THEN 'qlikview'::content_type_enum " +
                "WHEN \"Name\" ILIKE 'Html' THEN 'html'::content_type_enum " +
                "WHEN \"Name\" ILIKE 'Pdf' THEN 'pdf'::content_type_enum " +
                "WHEN \"Name\" ILIKE 'FileDownload' THEN 'file_download'::content_type_enum " +
                "WHEN \"Name\" ILIKE 'PowerBi' THEN 'power_bi'::content_type_enum " +
                "END "
                );

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ContentType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ContentType",
                type: "text",
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.Sql(
                "UPDATE \"ContentType\" " +
                "SET \"Name\" = CASE " +
                "WHEN \"TypeEnum\" = 'qlikview'::content_type_enum THEN 'Qlikview' " +
                "WHEN \"TypeEnum\" = 'html'::content_type_enum THEN 'Html' " +
                "WHEN \"TypeEnum\" = 'pdf'::content_type_enum THEN 'Pdf' " +
                "WHEN \"TypeEnum\" = 'file_download'::content_type_enum THEN 'FileDownload' " +
                "WHEN \"TypeEnum\" = 'power_bi'::content_type_enum THEN 'PowerBi' " +
                "END "
                );

            migrationBuilder.DropColumn(
                name: "TypeEnum",
                table: "ContentType");
        }
    }
}
