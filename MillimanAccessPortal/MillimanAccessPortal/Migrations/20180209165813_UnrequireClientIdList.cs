using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class UnrequireClientIdList : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long[]>(
                name: "ClientIdList",
                table: "RootContentItem",
                nullable: true,
                oldClrType: typeof(long[]));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long[]>(
                name: "ClientIdList",
                table: "RootContentItem",
                nullable: false,
                oldClrType: typeof(long[]),
                oldNullable: true);
        }
    }
}
