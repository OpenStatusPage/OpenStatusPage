using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.PostgreSql
{
    public partial class IncidentTimelineMerge : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentSeverityTimelineItems");

            migrationBuilder.DropTable(
                name: "IncidentStatusTimelineItems");

            migrationBuilder.CreateTable(
                name: "IncidentTimelineItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IncidentId = table.Column<string>(type: "text", nullable: false),
                    DateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AdditionalInformation = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentTimelineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentTimelineItems_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTimelineItems_IncidentId",
                table: "IncidentTimelineItems",
                column: "IncidentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentTimelineItems");

            migrationBuilder.CreateTable(
                name: "IncidentSeverityTimelineItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IncidentId = table.Column<string>(type: "text", nullable: false),
                    AdditionalInformation = table.Column<string>(type: "text", nullable: true),
                    DateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentSeverityTimelineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentSeverityTimelineItems_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentStatusTimelineItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IncidentId = table.Column<string>(type: "text", nullable: false),
                    AdditionalInformation = table.Column<string>(type: "text", nullable: true),
                    DateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentStatusTimelineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentStatusTimelineItems_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentSeverityTimelineItems_IncidentId",
                table: "IncidentSeverityTimelineItems",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentStatusTimelineItems_IncidentId",
                table: "IncidentStatusTimelineItems",
                column: "IncidentId");
        }
    }
}
