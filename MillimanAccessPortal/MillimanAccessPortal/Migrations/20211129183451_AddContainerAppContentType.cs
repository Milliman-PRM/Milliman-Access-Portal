using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddContainerAppContentType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TYPE content_type_enum ADD VALUE 'container_app'", suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TYPE content_type_enum RENAME TO content_type_enum_old; " +
                "CREATE TYPE content_type_enum AS ENUM('unknown', 'qlikview', 'html', 'pdf', 'file_download', 'power_bi'); " +
                "DELETE FROM \"ContentType\" WHERE \"TypeEnum\" = 'container_app'; " +
                "ALTER TABLE \"ContentType\" " +
                "   ALTER COLUMN \"TypeEnum\" DROP DEFAULT, " +
                "   ALTER COLUMN \"TypeEnum\" TYPE content_type_enum USING \"TypeEnum\"::text::content_type_enum, " +
                "   ALTER COLUMN \"TypeEnum\" SET DEFAULT 'unknown'; " +
                "DROP TYPE content_type_enum_old; ", 
                suppressTransaction: true);
        }
    }
}
