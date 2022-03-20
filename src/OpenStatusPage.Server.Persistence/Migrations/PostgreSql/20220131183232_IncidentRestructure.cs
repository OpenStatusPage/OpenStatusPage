using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.PostgreSql
{
    public partial class IncidentRestructure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Incidents");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Incidents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Incidents");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Incidents",
                type: "text",
                nullable: true);
        }
    }
}
