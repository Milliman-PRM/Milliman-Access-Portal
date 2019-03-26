using System;
using System.Collections.Generic;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddAuthenticationSchemes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPublicationRequestId",
                table: "ContentReductionTask");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:authentication_type", "default,ws_federation")
                .Annotation("Npgsql:Enum:content_type_enum", "unknown,qlikview,html,pdf,file_download")
                .Annotation("Npgsql:Enum:publication_status", "unknown,canceled,rejected,validating,queued,processing,post_process_ready,post_processing,processed,confirming,confirmed,replaced,error")
                .Annotation("Npgsql:Enum:reduction_status_enum", "unspecified,canceled,rejected,validating,queued,reducing,reduced,live,replaced,error")
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.Sql(
                "ALTER TABLE \"ContentReductionTask\" " +
                    "ALTER COLUMN \"ReductionStatus\" DROP DEFAULT, " +
                    "ALTER COLUMN \"ReductionStatus\" TYPE reduction_status_enum " +
                        "USING CASE \"ReductionStatus\" " +
                            "when 0 then 'unspecified' " +
                            "when 1 then 'canceled' " +
                            "when 2 then 'rejected' " +
                            "when 11 then 'validating' " +
                            "when 10 then 'queued' " +
                            "when 20 then 'reducing' " +
                            "when 30 then 'reduced' " +
                            "when 40 then 'live' " +
                            "when 50 then 'replaced' " +
                            "when 90 then 'error' " +
                        "END :: reduction_status_enum, " +
                    "ALTER COLUMN \"ReductionStatus\" SET DEFAULT 'unspecified' " +
                    "; ");
            /*
            migrationBuilder.AlterColumn<ReductionStatusEnum>(
                name: "ReductionStatus",
                table: "ContentReductionTask",
                nullable: false,
                defaultValue: ReductionStatusEnum.Unspecified,
                oldClrType: typeof(long),
                oldDefaultValue: 0L);
            */

            migrationBuilder.Sql(
                "ALTER TABLE \"ContentPublicationRequest\" " +
                    "ALTER COLUMN \"RequestStatus\" DROP DEFAULT, " +
                    "ALTER COLUMN \"RequestStatus\" TYPE publication_status " +
                        "USING CASE \"RequestStatus\" " +
                            "when 0 then 'unknown' " +
                            "when 1 then 'canceled' " +
                            "when 2 then 'rejected' " +
                            "when 9 then 'validating' " +
                            "when 10 then 'queued' " +
                            "when 20 then 'processing' " +
                            "when 25 then 'post_process_ready' " +
                            "when 27 then 'post_processing' " +
                            "when 30 then 'processed' " +
                            "when 35 then 'confirming' " +
                            "when 40 then 'confirmed' " +
                            "when 50 then 'replaced' " +
                            "when 90 then 'error' " +
                        "END :: publication_status, " +
                    "ALTER COLUMN \"RequestStatus\" SET DEFAULT 'unknown' " +
                    "; ");
            /*
            migrationBuilder.AlterColumn<PublicationStatus>(
                name: "RequestStatus",
                table: "ContentPublicationRequest",
                nullable: false,
                oldClrType: typeof(int));
            */

            migrationBuilder.AddColumn<Guid>(
                name: "AuthenticationSchemeId",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuthenticationScheme",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Name = table.Column<string>(nullable: false),
                    Type = table.Column<AuthenticationType>(nullable: false),
                    SchemeProperties = table.Column<string>(type: "jsonb", nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    DomainList = table.Column<List<string>>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationScheme", x => x.Id);
                    table.UniqueConstraint("AK_AuthenticationScheme_Name", x => x.Name);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AuthenticationSchemeId",
                table: "AspNetUsers",
                column: "AuthenticationSchemeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AuthenticationScheme_AuthenticationSchemeId",
                table: "AspNetUsers",
                column: "AuthenticationSchemeId",
                principalTable: "AuthenticationScheme",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPubli~",
                table: "ContentReductionTask",
                column: "ContentPublicationRequestId",
                principalTable: "ContentPublicationRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AuthenticationScheme_AuthenticationSchemeId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPubli~",
                table: "ContentReductionTask");

            migrationBuilder.DropTable(
                name: "AuthenticationScheme");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AuthenticationSchemeId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AuthenticationSchemeId",
                table: "AspNetUsers");

            migrationBuilder.Sql(
                "ALTER TABLE \"ContentReductionTask\" " +
                "ALTER COLUMN \"ReductionStatus\" DROP DEFAULT, " +
                "ALTER COLUMN \"ReductionStatus\" TYPE bigint " +
                    "USING CASE \"ReductionStatus\" " +
                        "when 'unspecified' then 0 " +
                        "when 'canceled' then 1 " +
                        "when 'rejected' then 2 " +
                        "when 'validating' then 11 " +
                        "when 'queued' then 10 " +
                        "when 'reducing' then 20 " +
                        "when 'reduced' then 30 " +
                        "when 'live' then 40 " +
                        "when 'replaced' then 50 " +
                        "when 'error' then 90 " +
                    "END :: bigint, " +
                "ALTER COLUMN \"ReductionStatus\" SET DEFAULT 0 " +
                "; ");
            /*
            migrationBuilder.AlterColumn<long>(
                name: "ReductionStatus",
                table: "ContentReductionTask",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(ReductionStatusEnum),
                oldDefaultValue: ReductionStatusEnum.Unspecified);
            */

            migrationBuilder.Sql(
                "ALTER TABLE \"ContentPublicationRequest\" " +
                    "ALTER COLUMN \"RequestStatus\" DROP DEFAULT, " +
                    "ALTER COLUMN \"RequestStatus\" TYPE integer " +
                        "USING CASE \"RequestStatus\" " +
                            "when 'unknown' then 0 " +
                            "when 'canceled' then 1 " +
                            "when 'rejected' then 2 " +
                            "when 'validating' then 9 " +
                            "when 'queued' then 10 " +
                            "when 'processing' then 20 " +
                            "when 'post_process_ready' then 25 " +
                            "when 'post_processing' then 27 " +
                            "when 'processed' then 30 " +
                            "when 'confirming' then 35 " +
                            "when 'confirmed' then 40 " +
                            "when 'replaced' then 50 " +
                            "when 'error' then 90 " +
                        "END :: integer, " +
                    "ALTER COLUMN \"RequestStatus\" SET DEFAULT 0 " +
                    "; ");
            /*
            migrationBuilder.AlterColumn<int>(
                name: "RequestStatus",
                table: "ContentPublicationRequest",
                nullable: false,
                oldClrType: typeof(PublicationStatus));
            */

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:authentication_type", "default,ws_federation")
                .OldAnnotation("Npgsql:Enum:content_type_enum", "unknown,qlikview,html,pdf,file_download")
                .OldAnnotation("Npgsql:Enum:publication_status", "unknown,canceled,rejected,validating,queued,processing,post_process_ready,post_processing,processed,confirming,confirmed,replaced,error")
                .OldAnnotation("Npgsql:Enum:reduction_status_enum", "unspecified,canceled,rejected,validating,queued,reducing,reduced,live,replaced,error")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPublicationRequestId",
                table: "ContentReductionTask",
                column: "ContentPublicationRequestId",
                principalTable: "ContentPublicationRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
