﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.PostgreSql
{
    public partial class SettingsFlushInterval : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "StatusFlushInterval",
                table: "ApplicationSettings",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusFlushInterval",
                table: "ApplicationSettings");
        }
    }
}
