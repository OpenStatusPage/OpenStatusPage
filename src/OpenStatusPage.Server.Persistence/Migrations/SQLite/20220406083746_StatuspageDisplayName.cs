using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.SQLite
{
    public partial class StatuspageDisplayName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "StatusPages",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "StatusPages");
        }
    }
}
