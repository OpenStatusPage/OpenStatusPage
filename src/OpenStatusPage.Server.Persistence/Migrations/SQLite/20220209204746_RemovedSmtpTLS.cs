using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.SQLite
{
    public partial class RemovedSmtpTLS : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseTls",
                table: "SmtpEmailProviders");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseTls",
                table: "SmtpEmailProviders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
