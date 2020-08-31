using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddClientReviewDueDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"CREATE OR REPLACE FUNCTION public.default_client_review_due() RETURNS timestamp without time zone AS $BODY$ {Environment.NewLine}" +
                $"DECLARE {Environment.NewLine}" +
                $"  earlyWarningDays int;{Environment.NewLine}  cmd text;{Environment.NewLine}  result text; {Environment.NewLine}" +
                $"BEGIN {Environment.NewLine}" +
                $"  SELECT \"Value\" INTO earlyWarningDays FROM \"NameValueConfiguration\" WHERE \"Key\" = 'ClientReviewRenewalPeriodDays'; {Environment.NewLine}" +
                $"  IF earlyWarningDays IS NULL THEN {Environment.NewLine}" +
                $"    RAISE 'ClientReviewEarlyWarningDays key not found in table ClientReviewRenewalPeriodDays'; {Environment.NewLine}" +
                $"  END IF; {Environment.NewLine}" +
                $"  cmd := 'SELECT now() at time zone ''utc'' + ''' || earlyWarningDays || ' d''::interval'; {Environment.NewLine}" +
                $"  EXECUTE cmd INTO result; {Environment.NewLine}" +
                $"  return result; {Environment.NewLine}" +
                $"END; {Environment.NewLine}" +
                $"$BODY$ LANGUAGE plpgsql; ");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewDueDateTimeUtc",
                table: "Client",
                nullable: false,
                defaultValueSql: "default_client_review_due()");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewDueDateTimeUtc",
                table: "Client");

            migrationBuilder.Sql("DROP FUNCTION public.default_client_review_due(); ");
        }
    }
}
