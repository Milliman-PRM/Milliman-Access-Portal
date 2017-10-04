using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class NewClientProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientCode",
                table: "Client",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsultantEmail",
                table: "Client",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsultantName",
                table: "Client",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsultantOffice",
                table: "Client",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Client",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "Client",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Client",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactTitle",
                table: "Client",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfitCenter",
                table: "Client",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientCode",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ConsultantEmail",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ConsultantName",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ConsultantOffice",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ContactTitle",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ProfitCenter",
                table: "Client");
        }
    }
}
