using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddNewUserWelcomeText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", "'uuid-ossp', '', ''");

            migrationBuilder.AlterColumn<string[]>(
                name: "AcceptedEmailAddressExceptionList",
                table: "Client",
                nullable: false,
                oldClrType: typeof(string[]),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewUserWelcomeText",
                table: "Client",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewUserWelcomeText",
                table: "Client");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:uuid-ossp", "'uuid-ossp', '', ''");

            migrationBuilder.AlterColumn<string[]>(
                name: "AcceptedEmailAddressExceptionList",
                table: "Client",
                nullable: true,
                oldClrType: typeof(string[]));
        }
    }
}
