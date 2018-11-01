using Microsoft.EntityFrameworkCore.Migrations;

namespace MillimanAccessPortal.Migrations
{
    public partial class ConvertArrayFieldsToList : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPublicationRequestId",
                table: "ContentReductionTask");

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
                name: "FK_ContentReductionTask_ContentPublicationRequest_ContentPubli~",
                table: "ContentReductionTask");

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
