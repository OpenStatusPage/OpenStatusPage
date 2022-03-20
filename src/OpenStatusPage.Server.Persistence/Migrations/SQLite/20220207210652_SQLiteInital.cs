using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.SQLite
{
    public partial class SQLiteInital : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClusterMembers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    From = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Until = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Monitors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Interval = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Retries = table.Column<ushort>(type: "INTEGER", nullable: true),
                    RetryInterval = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Timeout = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Monitors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationProviders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultForNewMonitors = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaftLogMetaEntries",
                columns: table => new
                {
                    Index = table.Column<long>(type: "INTEGER", nullable: false),
                    Term = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaftLogMetaEntries", x => x.Index);
                });

            migrationBuilder.CreateTable(
                name: "StatusPages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    EnableGlobalSummary = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableUpcomingMaintenances = table.Column<bool>(type: "INTEGER", nullable: false),
                    DaysUpcomingMaintenances = table.Column<int>(type: "INTEGER", nullable: true),
                    DaysStatusHistory = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableIncidentTimeline = table.Column<bool>(type: "INTEGER", nullable: false),
                    DaysIncidentTimeline = table.Column<int>(type: "INTEGER", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentTimelineItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    IncidentId = table.Column<string>(type: "TEXT", nullable: false),
                    DateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AdditionalInformation = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "DnsMonitors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Hostname = table.Column<string>(type: "TEXT", nullable: false),
                    Resolvers = table.Column<string>(type: "TEXT", nullable: true),
                    RecordType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DnsMonitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DnsMonitors_Monitors_Id",
                        column: x => x.Id,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HttpMonitors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxRedirects = table.Column<ushort>(type: "INTEGER", nullable: false),
                    Headers = table.Column<string>(type: "TEXT", nullable: true),
                    Body = table.Column<string>(type: "TEXT", nullable: true),
                    AuthenticationScheme = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthenticationBase = table.Column<string>(type: "TEXT", nullable: true),
                    AuthenticationAdditional = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HttpMonitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HttpMonitors_Monitors_Id",
                        column: x => x.Id,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentMonitorMappings",
                columns: table => new
                {
                    IncidentId = table.Column<string>(type: "TEXT", nullable: false),
                    MonitorBaseId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentMonitorMappings", x => new { x.MonitorBaseId, x.IncidentId });
                    table.ForeignKey(
                        name: "FK_IncidentMonitorMappings_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentMonitorMappings_Monitors_MonitorBaseId",
                        column: x => x.MonitorBaseId,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonitorRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MonitorId = table.Column<string>(type: "TEXT", nullable: false),
                    OrderIndex = table.Column<ushort>(type: "INTEGER", nullable: false),
                    ViolationStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitorRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonitorRules_Monitors_MonitorId",
                        column: x => x.MonitorId,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PingMonitors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Hostname = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PingMonitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PingMonitors_Monitors_Id",
                        column: x => x.Id,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SshMonitors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Hostname = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<ushort>(type: "INTEGER", nullable: true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: true),
                    PrivateKey = table.Column<string>(type: "TEXT", nullable: true),
                    Command = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SshMonitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SshMonitors_Monitors_Id",
                        column: x => x.Id,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TcpMonitors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Hostname = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<ushort>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TcpMonitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TcpMonitors_Monitors_Id",
                        column: x => x.Id,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UdpMonitors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Hostname = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<ushort>(type: "INTEGER", nullable: false),
                    RequestBytes = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UdpMonitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UdpMonitors_Monitors_Id",
                        column: x => x.Id,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonitorNotificationMappings",
                columns: table => new
                {
                    MonitorBaseId = table.Column<string>(type: "TEXT", nullable: false),
                    NotificationProviderId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitorNotificationMappings", x => new { x.MonitorBaseId, x.NotificationProviderId });
                    table.ForeignKey(
                        name: "FK_MonitorNotificationMappings_Monitors_MonitorBaseId",
                        column: x => x.MonitorBaseId,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonitorNotificationMappings_NotificationProviders_NotificationProviderId",
                        column: x => x.NotificationProviderId,
                        principalTable: "NotificationProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmtpEmailProviders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Hostname = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<ushort>(type: "INTEGER", nullable: true),
                    UseTls = table.Column<bool>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    FromAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ReceiversDirect = table.Column<string>(type: "TEXT", nullable: true),
                    ReceiversCC = table.Column<string>(type: "TEXT", nullable: true),
                    ReceiversBCC = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmtpEmailProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmtpEmailProviders_NotificationProviders_Id",
                        column: x => x.Id,
                        principalTable: "NotificationProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookProviders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Headers = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookProviders_NotificationProviders_Id",
                        column: x => x.Id,
                        principalTable: "NotificationProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DaysMonitorHistory = table.Column<ushort>(type: "INTEGER", nullable: false),
                    DaysIncidentHistory = table.Column<ushort>(type: "INTEGER", nullable: false),
                    DefaultStatusPageId = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationSettings_StatusPages_DefaultStatusPageId",
                        column: x => x.DefaultStatusPageId,
                        principalTable: "StatusPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonitorSummaries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    StatusPageId = table.Column<string>(type: "TEXT", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    ShowHistory = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitorSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonitorSummaries_StatusPages_StatusPageId",
                        column: x => x.StatusPageId,
                        principalTable: "StatusPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DnsRecordRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ComparisonValue = table.Column<string>(type: "TEXT", nullable: false),
                    ComparisonType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DnsRecordRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DnsRecordRules_MonitorRules_Id",
                        column: x => x.Id,
                        principalTable: "MonitorRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResponseBodyRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ComparisonValue = table.Column<string>(type: "TEXT", nullable: false),
                    ComparisonType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseBodyRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseBodyRules_MonitorRules_Id",
                        column: x => x.Id,
                        principalTable: "MonitorRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResponseBytesRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ExpectedBytes = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ComparisonType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseBytesRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseBytesRules_MonitorRules_Id",
                        column: x => x.Id,
                        principalTable: "MonitorRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResponseHeaderRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    ComparisonValue = table.Column<string>(type: "TEXT", nullable: true),
                    ComparisonType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseHeaderRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseHeaderRules_MonitorRules_Id",
                        column: x => x.Id,
                        principalTable: "MonitorRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResponseTimeRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ComparisonValue = table.Column<ushort>(type: "INTEGER", nullable: false),
                    ComparisonType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseTimeRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseTimeRules_MonitorRules_Id",
                        column: x => x.Id,
                        principalTable: "MonitorRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SshCommandResultRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ComparisonValue = table.Column<string>(type: "TEXT", nullable: false),
                    ComparisonType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SshCommandResultRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SshCommandResultRules_MonitorRules_Id",
                        column: x => x.Id,
                        principalTable: "MonitorRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SslCertificateRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CheckType = table.Column<int>(type: "INTEGER", nullable: false),
                    MinValidTimespan = table.Column<TimeSpan>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SslCertificateRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SslCertificateRules_MonitorRules_Id",
                        column: x => x.Id,
                        principalTable: "MonitorRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatusCodeRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<ushort>(type: "INTEGER", nullable: false),
                    UpperRangeValue = table.Column<ushort>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusCodeRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatusCodeRules_MonitorRules_Id",
                        column: x => x.Id,
                        principalTable: "MonitorRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabeledMonitors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MonitorSummaryId = table.Column<string>(type: "TEXT", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    MonitorId = table.Column<string>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabeledMonitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabeledMonitors_Monitors_MonitorId",
                        column: x => x.MonitorId,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabeledMonitors_MonitorSummaries_MonitorSummaryId",
                        column: x => x.MonitorSummaryId,
                        principalTable: "MonitorSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSettings_DefaultStatusPageId",
                table: "ApplicationSettings",
                column: "DefaultStatusPageId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentMonitorMappings_IncidentId",
                table: "IncidentMonitorMappings",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTimelineItems_IncidentId",
                table: "IncidentTimelineItems",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_LabeledMonitors_MonitorId",
                table: "LabeledMonitors",
                column: "MonitorId");

            migrationBuilder.CreateIndex(
                name: "IX_LabeledMonitors_MonitorSummaryId",
                table: "LabeledMonitors",
                column: "MonitorSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitorNotificationMappings_NotificationProviderId",
                table: "MonitorNotificationMappings",
                column: "NotificationProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitorRules_MonitorId",
                table: "MonitorRules",
                column: "MonitorId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitorSummaries_StatusPageId",
                table: "MonitorSummaries",
                column: "StatusPageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationSettings");

            migrationBuilder.DropTable(
                name: "ClusterMembers");

            migrationBuilder.DropTable(
                name: "DnsMonitors");

            migrationBuilder.DropTable(
                name: "DnsRecordRules");

            migrationBuilder.DropTable(
                name: "HttpMonitors");

            migrationBuilder.DropTable(
                name: "IncidentMonitorMappings");

            migrationBuilder.DropTable(
                name: "IncidentTimelineItems");

            migrationBuilder.DropTable(
                name: "LabeledMonitors");

            migrationBuilder.DropTable(
                name: "MonitorNotificationMappings");

            migrationBuilder.DropTable(
                name: "PingMonitors");

            migrationBuilder.DropTable(
                name: "RaftLogMetaEntries");

            migrationBuilder.DropTable(
                name: "ResponseBodyRules");

            migrationBuilder.DropTable(
                name: "ResponseBytesRules");

            migrationBuilder.DropTable(
                name: "ResponseHeaderRules");

            migrationBuilder.DropTable(
                name: "ResponseTimeRules");

            migrationBuilder.DropTable(
                name: "SmtpEmailProviders");

            migrationBuilder.DropTable(
                name: "SshCommandResultRules");

            migrationBuilder.DropTable(
                name: "SshMonitors");

            migrationBuilder.DropTable(
                name: "SslCertificateRules");

            migrationBuilder.DropTable(
                name: "StatusCodeRules");

            migrationBuilder.DropTable(
                name: "TcpMonitors");

            migrationBuilder.DropTable(
                name: "UdpMonitors");

            migrationBuilder.DropTable(
                name: "WebhookProviders");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "MonitorSummaries");

            migrationBuilder.DropTable(
                name: "MonitorRules");

            migrationBuilder.DropTable(
                name: "NotificationProviders");

            migrationBuilder.DropTable(
                name: "StatusPages");

            migrationBuilder.DropTable(
                name: "Monitors");
        }
    }
}
