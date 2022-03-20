using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.PostgreSql
{
    public partial class ConsensusPersistency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClusterMembers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaftLogMetaEntries",
                columns: table => new
                {
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Term = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaftLogMetaEntries", x => x.Index);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClusterMembers");

            migrationBuilder.DropTable(
                name: "RaftLogMetaEntries");
        }
    }
}
