using System.Collections.Generic;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class FileDropHashAndNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<HashSet<FileDropUserNotificationModel>>(
                name: "NotificationSubscriptions",
                table: "SftpAccount",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortHash",
                table: "FileDrop",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FileDrop_ShortHash",
                table: "FileDrop",
                column: "ShortHash",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileDrop_ShortHash",
                table: "FileDrop");

            migrationBuilder.DropColumn(
                name: "NotificationSubscriptions",
                table: "SftpAccount");

            migrationBuilder.DropColumn(
                name: "ShortHash",
                table: "FileDrop");
        }
    }
}
