using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddFileDropSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileDrop",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    RootPath = table.Column<string>(nullable: true),
                    ClientId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileDrop", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileDrop_FileDrop_ClientId",
                        column: x => x.ClientId,
                        principalTable: "FileDrop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SftpAccount",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserName = table.Column<string>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: false),
                    ReadAccess = table.Column<bool>(nullable: false),
                    WriteAccess = table.Column<bool>(nullable: false),
                    DeleteAccess = table.Column<bool>(nullable: false),
                    ApplicationUserId = table.Column<Guid>(nullable: true),
                    FileDropId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SftpAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SftpAccount_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SftpAccount_FileDrop_FileDropId",
                        column: x => x.FileDropId,
                        principalTable: "FileDrop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SftpConnection",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SftpAccountId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SftpConnection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SftpConnection_FileDrop_SftpAccountId",
                        column: x => x.SftpAccountId,
                        principalTable: "FileDrop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileDrop_ClientId",
                table: "FileDrop",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SftpAccount_ApplicationUserId",
                table: "SftpAccount",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SftpAccount_FileDropId",
                table: "SftpAccount",
                column: "FileDropId");

            migrationBuilder.CreateIndex(
                name: "IX_SftpConnection_SftpAccountId",
                table: "SftpConnection",
                column: "SftpAccountId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SftpAccount");

            migrationBuilder.DropTable(
                name: "SftpConnection");

            migrationBuilder.DropTable(
                name: "FileDrop");
        }
    }
}
