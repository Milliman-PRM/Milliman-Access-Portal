using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class RequireContentTypeFileExtensions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string[]>(
                name: "FileExtensions",
                table: "ContentType",
                nullable: false,
                oldClrType: typeof(string[]),
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string[]>(
                name: "FileExtensions",
                table: "ContentType",
                nullable: true,
                oldClrType: typeof(string[]));
        }
    }
}
