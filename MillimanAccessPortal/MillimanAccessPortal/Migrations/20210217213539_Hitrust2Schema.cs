using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class Hitrust2Schema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UserAgreementAcceptedUtc",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"AspNetUsers\" " +
                "SET \"UserAgreementAcceptedUtc\" = now() at time zone 'utc' - interval '365 days' " +
                "WHERE \"IsUserAgreementAccepted\" IS NOT NULL ");

            migrationBuilder.DropColumn(
                name: "IsUserAgreementAccepted",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<List<string>>(
                name: "QuarterlyMaintenanceNotificationList",
                table: "ProfitCenter",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastQuarterlyMaintenanceNotificationUtc",
                table: "ProfitCenter",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastQuarterlyMaintenanceNotificationUtc",
                table: "ProfitCenter");

            migrationBuilder.DropColumn(
                name: "QuarterlyMaintenanceNotificationList",
                table: "ProfitCenter");

            migrationBuilder.AddColumn<bool>(
                name: "IsUserAgreementAccepted",
                table: "AspNetUsers",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"AspNetUsers\" " +
                "SET \"IsUserAgreementAccepted\" = true " +
                "WHERE \"UserAgreementAcceptedUtc\" IS NOT NULL " +
                "AND \"UserAgreementAcceptedUtc\" > now() at time zone 'utc' - interval '365 days' ");

            migrationBuilder.Sql("UPDATE \"AspNetUsers\" " +
                "SET \"IsUserAgreementAccepted\" = false " +
                "WHERE \"UserAgreementAcceptedUtc\" IS NOT NULL " +
                "AND \"UserAgreementAcceptedUtc\" < now() at time zone 'utc' - interval '365 days' ");

            migrationBuilder.DropColumn(
                name: "UserAgreementAcceptedUtc",
                table: "AspNetUsers");
        }
    }
}
