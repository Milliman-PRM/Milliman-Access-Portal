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
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    IsSuspended = table.Column<bool>(nullable: false),
                    RootPath = table.Column<string>(type: "citext", nullable: false),
                    ClientId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileDrop", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileDrop_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileDropDirectory",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CanonicalFileDropPath = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    ParentDirectoryId = table.Column<Guid>(nullable: true),
                    FileDropId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileDropDirectory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileDropDirectory_FileDrop_FileDropId",
                        column: x => x.FileDropId,
                        principalTable: "FileDrop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileDropDirectory_FileDropDirectory_ParentDirectoryId",
                        column: x => x.ParentDirectoryId,
                        principalTable: "FileDropDirectory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileDropUserPermissionGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Name = table.Column<string>(nullable: false),
                    ReadAccess = table.Column<bool>(nullable: false),
                    WriteAccess = table.Column<bool>(nullable: false),
                    DeleteAccess = table.Column<bool>(nullable: false),
                    IsPersonalGroup = table.Column<bool>(nullable: false),
                    FileDropId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileDropUserPermissionGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileDropUserPermissionGroup_FileDrop_FileDropId",
                        column: x => x.FileDropId,
                        principalTable: "FileDrop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SftpAccount",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    UserName = table.Column<string>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    PasswordResetDateTimeUtc = table.Column<DateTime>(nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)),
                    IsSuspended = table.Column<bool>(nullable: false),
                    ApplicationUserId = table.Column<Guid>(nullable: true),
                    FileDropUserPermissionGroupId = table.Column<Guid>(nullable: true),
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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SftpAccount_FileDrop_FileDropId",
                        column: x => x.FileDropId,
                        principalTable: "FileDrop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SftpAccount_FileDropUserPermissionGroup_FileDropUserPermiss~",
                        column: x => x.FileDropUserPermissionGroupId,
                        principalTable: "FileDropUserPermissionGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FileDropFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    FileName = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    DirectoryId = table.Column<Guid>(nullable: false),
                    CreatedByAccountId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileDropFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileDropFile_SftpAccount_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "SftpAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileDropFile_FileDropDirectory_DirectoryId",
                        column: x => x.DirectoryId,
                        principalTable: "FileDropDirectory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileDrop_ClientId",
                table: "FileDrop",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_FileDrop_RootPath",
                table: "FileDrop",
                column: "RootPath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileDropDirectory_ParentDirectoryId",
                table: "FileDropDirectory",
                column: "ParentDirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FileDropDirectory_FileDropId_CanonicalFileDropPath",
                table: "FileDropDirectory",
                columns: new[] { "FileDropId", "CanonicalFileDropPath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileDropFile_CreatedByAccountId",
                table: "FileDropFile",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FileDropFile_DirectoryId_FileName",
                table: "FileDropFile",
                columns: new[] { "DirectoryId", "FileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileDropUserPermissionGroup_FileDropId",
                table: "FileDropUserPermissionGroup",
                column: "FileDropId");

            migrationBuilder.CreateIndex(
                name: "IX_SftpAccount_ApplicationUserId",
                table: "SftpAccount",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SftpAccount_FileDropId",
                table: "SftpAccount",
                column: "FileDropId");

            migrationBuilder.CreateIndex(
                name: "IX_SftpAccount_FileDropUserPermissionGroupId",
                table: "SftpAccount",
                column: "FileDropUserPermissionGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileDropFile");

            migrationBuilder.DropTable(
                name: "SftpAccount");

            migrationBuilder.DropTable(
                name: "FileDropDirectory");

            migrationBuilder.DropTable(
                name: "FileDropUserPermissionGroup");

            migrationBuilder.DropTable(
                name: "FileDrop");
        }
    }
}
