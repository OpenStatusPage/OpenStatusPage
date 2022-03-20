using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.SQLite
{
    public partial class MonitorWorkerCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkerCount",
                table: "Monitors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkerCount",
                table: "Monitors");
        }
    }
}
