using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.PostgreSql
{
    public partial class IncidentMappingsFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IncidentMonitorMappings_Monitors_AffectedServicesId",
                table: "IncidentMonitorMappings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IncidentMonitorMappings",
                table: "IncidentMonitorMappings");

            migrationBuilder.DropIndex(
                name: "IX_IncidentMonitorMappings_AffectedServicesId",
                table: "IncidentMonitorMappings");

            migrationBuilder.DropColumn(
                name: "MonitorId",
                table: "IncidentMonitorMappings");

            migrationBuilder.RenameColumn(
                name: "AffectedServicesId",
                table: "IncidentMonitorMappings",
                newName: "MonitorBaseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IncidentMonitorMappings",
                table: "IncidentMonitorMappings",
                columns: new[] { "MonitorBaseId", "IncidentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentMonitorMappings_Monitors_MonitorBaseId",
                table: "IncidentMonitorMappings",
                column: "MonitorBaseId",
                principalTable: "Monitors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IncidentMonitorMappings_Monitors_MonitorBaseId",
                table: "IncidentMonitorMappings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IncidentMonitorMappings",
                table: "IncidentMonitorMappings");

            migrationBuilder.RenameColumn(
                name: "MonitorBaseId",
                table: "IncidentMonitorMappings",
                newName: "AffectedServicesId");

            migrationBuilder.AddColumn<string>(
                name: "MonitorId",
                table: "IncidentMonitorMappings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IncidentMonitorMappings",
                table: "IncidentMonitorMappings",
                columns: new[] { "MonitorId", "IncidentId" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentMonitorMappings_AffectedServicesId",
                table: "IncidentMonitorMappings",
                column: "AffectedServicesId");

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentMonitorMappings_Monitors_AffectedServicesId",
                table: "IncidentMonitorMappings",
                column: "AffectedServicesId",
                principalTable: "Monitors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
