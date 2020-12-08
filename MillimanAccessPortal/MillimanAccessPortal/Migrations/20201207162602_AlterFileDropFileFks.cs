using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class AlterFileDropFileFks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileDropFile_SftpAccount_CreatedByAccountId",
                table: "FileDropFile");

            migrationBuilder.DropForeignKey(
                name: "FK_FileDropFile_FileDropDirectory_DirectoryId",
                table: "FileDropFile");

            migrationBuilder.DropIndex(
                name: "IX_FileDropFile_CreatedByAccountId",
                table: "FileDropFile");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByAccountUserName",
                table: "FileDropFile",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"FileDropFile\" this " +
                "SET \"CreatedByAccountUserName\" = a.\"UserName\" " +
                "FROM \"FileDropFile\" f JOIN \"SftpAccount\" a ON a.\"Id\" = f.\"CreatedByAccountId\" " +
                "WHERE this.\"Id\" = f.\"Id\"; "
                );

            migrationBuilder.DropColumn(
                name: "CreatedByAccountId",
                table: "FileDropFile");

            migrationBuilder.AddForeignKey(
                name: "FK_FileDropFile_FileDropDirectory_DirectoryId",
                table: "FileDropFile",
                column: "DirectoryId",
                principalTable: "FileDropDirectory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileDropFile_FileDropDirectory_DirectoryId",
                table: "FileDropFile");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAccountId",
                table: "FileDropFile",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileDropFile_CreatedByAccountId",
                table: "FileDropFile",
                column: "CreatedByAccountId");

            migrationBuilder.Sql(
                "UPDATE \"FileDropFile\" this " +
                "SET \"CreatedByAccountId\" = a.\"Id\" " +
                "FROM \"FileDropFile\" f JOIN \"SftpAccount\" a ON a.\"UserName\" = f.\"CreatedByAccountUserName\" " +
                "WHERE this.\"CreatedByAccountUserName\" = f.\"CreatedByAccountUserName\"; "
                );

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedByAccountId",
                table: "FileDropFile",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FileDropFile_SftpAccount_CreatedByAccountId",
                table: "FileDropFile",
                column: "CreatedByAccountId",
                principalTable: "SftpAccount",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FileDropFile_FileDropDirectory_DirectoryId",
                table: "FileDropFile",
                column: "DirectoryId",
                principalTable: "FileDropDirectory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "CreatedByAccountUserName",
                table: "FileDropFile");
        }
    }
}
