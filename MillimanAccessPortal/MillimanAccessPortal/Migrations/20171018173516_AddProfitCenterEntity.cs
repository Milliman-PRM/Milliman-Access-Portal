using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddProfitCenterEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfitCenter",
                table: "Client");

            migrationBuilder.AddColumn<long>(
                name: "ProfitCenterId",
                table: "Client",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "ProfitCenter",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ContactEmail = table.Column<string>(nullable: true),
                    ContactName = table.Column<string>(nullable: true),
                    ContactPhone = table.Column<string>(nullable: true),
                    ContactTitle = table.Column<string>(nullable: true),
                    MillimanOffice = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: false),
                    ProfitCenterCode = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitCenter", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Client_ProfitCenterId",
                table: "Client",
                column: "ProfitCenterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Client_ProfitCenter_ProfitCenterId",
                table: "Client",
                column: "ProfitCenterId",
                principalTable: "ProfitCenter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Client_ProfitCenter_ProfitCenterId",
                table: "Client");

            migrationBuilder.DropTable(
                name: "ProfitCenter");

            migrationBuilder.DropIndex(
                name: "IX_Client_ProfitCenterId",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ProfitCenterId",
                table: "Client");

            migrationBuilder.AddColumn<string>(
                name: "ProfitCenter",
                table: "Client",
                nullable: true);
        }
    }
}
