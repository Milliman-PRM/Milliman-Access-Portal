using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddPowerBiContentType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TYPE content_type_enum ADD VALUE 'power_bi'", suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TYPE content_type_enum RENAME TO content_type_enum_old; " +
                "CREATE TYPE content_type_enum AS ENUM('unknown','qlikview','html','pdf','file_download'); " +
                "DROP TYPE content_type_enum_old;", suppressTransaction: true);
        }
    }
}
