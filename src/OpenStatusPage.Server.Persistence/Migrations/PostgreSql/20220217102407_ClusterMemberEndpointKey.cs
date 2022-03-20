using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.PostgreSql
{
    public partial class ClusterMemberEndpointKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ClusterMembers",
                table: "ClusterMembers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ClusterMembers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClusterMembers",
                table: "ClusterMembers",
                column: "Endpoint");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ClusterMembers",
                table: "ClusterMembers");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "ClusterMembers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClusterMembers",
                table: "ClusterMembers",
                column: "Id");
        }
    }
}
