using System;
using System.Collections.Generic;
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
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:uuid-ossp", "'uuid-ossp', '', ''");

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
                    Type = table.Column<int>(nullable: false),
                    SchemeProperties = table.Column<string>(type: "jsonb", nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    DomainList = table.Column<List<string>>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationScheme", x => x.Id);
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

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", "'uuid-ossp', '', ''")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

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
