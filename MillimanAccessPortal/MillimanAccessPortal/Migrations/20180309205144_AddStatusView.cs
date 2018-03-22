using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Migrations
{
    public partial class AddStatusView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE VIEW ""ContentPublicationRequestStatus"" AS
WITH BaseTable AS(
    SELECT ""ContentPublicationRequestId"", ARRAY_AGG(DISTINCT ""ReductionStatus"")::int[] as status_list
    FROM ""ContentReductionTask""
    GROUP BY ""ContentPublicationRequestId""
)
SELECT ""ContentPublicationRequestId"",
    CASE
        WHEN 90 = ANY(status_list) THEN 90  -- One or more tasks has the Error status
        WHEN status_list <@ ARRAY[40, 3] THEN 40  -- All tasks are pushed or replaced(return Pushed)
        WHEN array_length(status_list, 1) = 1 THEN status_list[1] -- All task statuses are the same; return whatever that status is
	    WHEN status_list <@ ARRAY[10, 20, 30] THEN 20  -- All tasks are
        ELSE 0  -- Default
    END as ""PublicationRequestStatus""
From BaseTable;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""ContentPublicationRequestStatus"";");
        }
    }
}
