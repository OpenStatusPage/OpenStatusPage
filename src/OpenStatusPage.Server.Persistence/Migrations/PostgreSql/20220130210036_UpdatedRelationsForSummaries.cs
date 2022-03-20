using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.PostgreSql
{
    public partial class UpdatedRelationsForSummaries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabeledMonitors_MonitorSummaries_MonitorSummaryId",
                table: "LabeledMonitors");

            migrationBuilder.DropForeignKey(
                name: "FK_MonitorSummaries_StatusPages_StatusPageId",
                table: "MonitorSummaries");

            migrationBuilder.AlterColumn<string>(
                name: "StatusPageId",
                table: "MonitorSummaries",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MonitorSummaryId",
                table: "LabeledMonitors",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LabeledMonitors_MonitorSummaries_MonitorSummaryId",
                table: "LabeledMonitors",
                column: "MonitorSummaryId",
                principalTable: "MonitorSummaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MonitorSummaries_StatusPages_StatusPageId",
                table: "MonitorSummaries",
                column: "StatusPageId",
                principalTable: "StatusPages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabeledMonitors_MonitorSummaries_MonitorSummaryId",
                table: "LabeledMonitors");

            migrationBuilder.DropForeignKey(
                name: "FK_MonitorSummaries_StatusPages_StatusPageId",
                table: "MonitorSummaries");

            migrationBuilder.AlterColumn<string>(
                name: "StatusPageId",
                table: "MonitorSummaries",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "MonitorSummaryId",
                table: "LabeledMonitors",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_LabeledMonitors_MonitorSummaries_MonitorSummaryId",
                table: "LabeledMonitors",
                column: "MonitorSummaryId",
                principalTable: "MonitorSummaries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MonitorSummaries_StatusPages_StatusPageId",
                table: "MonitorSummaries",
                column: "StatusPageId",
                principalTable: "StatusPages",
                principalColumn: "Id");
        }
    }
}
