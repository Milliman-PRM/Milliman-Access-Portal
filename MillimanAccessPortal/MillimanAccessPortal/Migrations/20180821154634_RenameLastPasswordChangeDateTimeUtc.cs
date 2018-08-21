using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class RenameLastPasswordChangeDateTimeUtc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PasswordChangeDate",
                table: "AspNetUsers",
                newName: "LastPasswordChangeDateTimeUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastPasswordChangeDateTimeUtc",
                table: "AspNetUsers",
                newName: "PasswordChangeDate");
        }
    }
}
