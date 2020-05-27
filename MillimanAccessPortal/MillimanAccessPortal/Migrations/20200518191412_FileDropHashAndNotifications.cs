using System.Collections.Generic;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class FileDropHashAndNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:authentication_type", "default,ws_federation")
                .Annotation("Npgsql:Enum:content_type_enum", "unknown,qlikview,html,pdf,file_download,power_bi")
                .Annotation("Npgsql:Enum:file_drop_notification_type", "file_write,file_read,file_delete")
                .Annotation("Npgsql:Enum:publication_status", "unknown,canceled,rejected,validating,queued,processing,post_process_ready,post_processing,processed,confirming,confirmed,replaced,error")
                .Annotation("Npgsql:Enum:reduction_status_enum", "unspecified,canceled,rejected,validating,queued,reducing,reduced,live,replaced,warning,error")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .OldAnnotation("Npgsql:Enum:authentication_type", "default,ws_federation")
                .OldAnnotation("Npgsql:Enum:content_type_enum", "unknown,qlikview,html,pdf,file_download,power_bi")
                .OldAnnotation("Npgsql:Enum:publication_status", "unknown,canceled,rejected,validating,queued,processing,post_process_ready,post_processing,processed,confirming,confirmed,replaced,error")
                .OldAnnotation("Npgsql:Enum:reduction_status_enum", "unspecified,canceled,rejected,validating,queued,reducing,reduced,live,replaced,warning,error")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

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

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:authentication_type", "default,ws_federation")
                .Annotation("Npgsql:Enum:content_type_enum", "unknown,qlikview,html,pdf,file_download,power_bi")
                .Annotation("Npgsql:Enum:publication_status", "unknown,canceled,rejected,validating,queued,processing,post_process_ready,post_processing,processed,confirming,confirmed,replaced,error")
                .Annotation("Npgsql:Enum:reduction_status_enum", "unspecified,canceled,rejected,validating,queued,reducing,reduced,live,replaced,warning,error")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .OldAnnotation("Npgsql:Enum:authentication_type", "default,ws_federation")
                .OldAnnotation("Npgsql:Enum:content_type_enum", "unknown,qlikview,html,pdf,file_download,power_bi")
                .OldAnnotation("Npgsql:Enum:file_drop_notification_type", "file_write,file_read,file_delete")
                .OldAnnotation("Npgsql:Enum:publication_status", "unknown,canceled,rejected,validating,queued,processing,post_process_ready,post_processing,processed,confirming,confirmed,replaced,error")
                .OldAnnotation("Npgsql:Enum:reduction_status_enum", "unspecified,canceled,rejected,validating,queued,reducing,reduced,live,replaced,warning,error")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:uuid-ossp", ",,");
        }
    }
}
