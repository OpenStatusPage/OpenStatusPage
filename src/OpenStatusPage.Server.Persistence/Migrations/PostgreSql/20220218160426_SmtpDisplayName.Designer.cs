﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OpenStatusPage.Server.Persistence.Drivers;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.PostgreSql
{
    [DbContext(typeof(PostgreSqlDbContext))]
    [Migration("20220218160426_SmtpDisplayName")]
    partial class SmtpDisplayName
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.ApplicationSettings", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<int>("DaysIncidentHistory")
                        .HasColumnType("integer");

                    b.Property<int>("DaysMonitorHistory")
                        .HasColumnType("integer");

                    b.Property<string>("DefaultStatusPageId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("DefaultStatusPageId");

                    b.ToTable("ApplicationSettings");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Cluster.ClusterMember", b =>
                {
                    b.Property<string>("Endpoint")
                        .HasColumnType("text");

                    b.HasKey("Endpoint");

                    b.ToTable("ClusterMembers");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Cluster.RaftLogMetaEntry", b =>
                {
                    b.Property<long>("Index")
                        .HasColumnType("bigint");

                    b.Property<long>("Term")
                        .HasColumnType("bigint");

                    b.HasKey("Index");

                    b.ToTable("RaftLogMetaEntries");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Incidents.Incident", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("From")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset?>("Until")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("Incidents");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Incidents.IncidentTimelineItem", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("AdditionalInformation")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("DateTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("IncidentId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Severity")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("IncidentId");

                    b.ToTable("IncidentTimelineItems", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<bool>("Enabled")
                        .HasColumnType("boolean");

                    b.Property<TimeSpan>("Interval")
                        .HasColumnType("interval");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int?>("Retries")
                        .HasColumnType("integer");

                    b.Property<TimeSpan?>("RetryInterval")
                        .HasColumnType("interval");

                    b.Property<string>("Tags")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<TimeSpan?>("Timeout")
                        .HasColumnType("interval");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.Property<int>("WorkerCount")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Monitors");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("MonitorId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("OrderIndex")
                        .HasColumnType("integer");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.Property<int>("ViolationStatus")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("MonitorId");

                    b.ToTable("MonitorRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.NotificationProviders.NotificationProvider", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<bool>("DefaultForNewMonitors")
                        .HasColumnType("boolean");

                    b.Property<bool>("Enabled")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("NotificationProviders");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Notifications.History.NotificationHistoryRecord", b =>
                {
                    b.Property<string>("MonitorId")
                        .HasColumnType("text");

                    b.Property<DateTime>("StatusUtc")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("MonitorId", "StatusUtc");

                    b.ToTable("NotificationHistoryRecords");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusHistory.StatusHistoryRecord", b =>
                {
                    b.Property<string>("MonitorId")
                        .HasColumnType("text");

                    b.Property<DateTime>("FromUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.HasKey("MonitorId", "FromUtc");

                    b.ToTable("StatusHistoryRecords");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusPages.LabeledMonitor", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("MonitorId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("MonitorSummaryId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("OrderIndex")
                        .HasColumnType("integer");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("MonitorId");

                    b.HasIndex("MonitorSummaryId");

                    b.ToTable("LabeledMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusPages.MonitorSummary", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<int>("OrderIndex")
                        .HasColumnType("integer");

                    b.Property<bool>("ShowHistory")
                        .HasColumnType("boolean");

                    b.Property<string>("StatusPageId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("StatusPageId");

                    b.ToTable("MonitorSummaries", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusPages.StatusPage", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<int?>("DaysIncidentTimeline")
                        .HasColumnType("integer");

                    b.Property<int>("DaysStatusHistory")
                        .HasColumnType("integer");

                    b.Property<int?>("DaysUpcomingMaintenances")
                        .HasColumnType("integer");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<bool>("EnableGlobalSummary")
                        .HasColumnType("boolean");

                    b.Property<bool>("EnableIncidentTimeline")
                        .HasColumnType("boolean");

                    b.Property<bool>("EnableUpcomingMaintenances")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Password")
                        .HasColumnType("text");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("StatusPages");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Persistence.Configurations.IncidentConfiguration+IncidentMonitorMapping", b =>
                {
                    b.Property<string>("MonitorBaseId")
                        .HasColumnType("text");

                    b.Property<string>("IncidentId")
                        .HasColumnType("text");

                    b.HasKey("MonitorBaseId", "IncidentId");

                    b.HasIndex("IncidentId");

                    b.ToTable("IncidentMonitorMappings", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Persistence.Configurations.MonitorConfiguration+MonitorNotificationMapping", b =>
                {
                    b.Property<string>("MonitorBaseId")
                        .HasColumnType("text");

                    b.Property<string>("NotificationProviderId")
                        .HasColumnType("text");

                    b.HasKey("MonitorBaseId", "NotificationProviderId");

                    b.HasIndex("NotificationProviderId");

                    b.ToTable("MonitorNotificationMappings", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Dns.DnsMonitor", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("RecordType")
                        .HasColumnType("integer");

                    b.Property<string>("Resolvers")
                        .HasColumnType("text");

                    b.ToTable("DnsMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Dns.DnsRecordRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("ComparisonType")
                        .HasColumnType("integer");

                    b.Property<string>("ComparisonValue")
                        .IsRequired()
                        .HasColumnType("text");

                    b.ToTable("DnsRecordRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.HttpMonitor", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase");

                    b.Property<string>("AuthenticationAdditional")
                        .HasColumnType("text");

                    b.Property<string>("AuthenticationBase")
                        .HasColumnType("text");

                    b.Property<int>("AuthenticationScheme")
                        .HasColumnType("integer");

                    b.Property<string>("Body")
                        .HasColumnType("text");

                    b.Property<string>("Headers")
                        .HasColumnType("text");

                    b.Property<int>("MaxRedirects")
                        .HasColumnType("integer");

                    b.Property<int>("Method")
                        .HasColumnType("integer");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("text");

                    b.ToTable("HttpMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.ResponseBodyRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("ComparisonType")
                        .HasColumnType("integer");

                    b.Property<string>("ComparisonValue")
                        .IsRequired()
                        .HasColumnType("text");

                    b.ToTable("ResponseBodyRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.ResponseHeaderRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("ComparisonType")
                        .HasColumnType("integer");

                    b.Property<string>("ComparisonValue")
                        .HasColumnType("text");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("text");

                    b.ToTable("ResponseHeaderRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.SslCertificateRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("CheckType")
                        .HasColumnType("integer");

                    b.Property<TimeSpan?>("MinValidTimespan")
                        .HasColumnType("interval");

                    b.ToTable("SslCertificateRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.StatusCodeRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int?>("UpperRangeValue")
                        .HasColumnType("integer");

                    b.Property<int>("Value")
                        .HasColumnType("integer");

                    b.ToTable("StatusCodeRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Ping.PingMonitor", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("text");

                    b.ToTable("PingMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.ResponseTimeRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("ComparisonType")
                        .HasColumnType("integer");

                    b.Property<int>("ComparisonValue")
                        .HasColumnType("integer");

                    b.ToTable("ResponseTimeRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Ssh.SshCommandResultRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("ComparisonType")
                        .HasColumnType("integer");

                    b.Property<string>("ComparisonValue")
                        .IsRequired()
                        .HasColumnType("text");

                    b.ToTable("SshCommandResultRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Ssh.SshMonitor", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase");

                    b.Property<string>("Command")
                        .HasColumnType("text");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Password")
                        .HasColumnType("text");

                    b.Property<int?>("Port")
                        .HasColumnType("integer");

                    b.Property<string>("PrivateKey")
                        .HasColumnType("text");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.ToTable("SshMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Tcp.TcpMonitor", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Port")
                        .HasColumnType("integer");

                    b.ToTable("TcpMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Udp.ResponseBytesRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("ComparisonType")
                        .HasColumnType("integer");

                    b.Property<byte[]>("ExpectedBytes")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.ToTable("ResponseBytesRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Udp.UdpMonitor", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Port")
                        .HasColumnType("integer");

                    b.Property<byte[]>("RequestBytes")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.ToTable("UdpMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.NotificationProviders.SmtpEmailProvider", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.NotificationProviders.NotificationProvider");

                    b.Property<string>("DisplayName")
                        .HasColumnType("text");

                    b.Property<string>("FromAddress")
                        .HasColumnType("text");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int?>("Port")
                        .HasColumnType("integer");

                    b.Property<string>("ReceiversBCC")
                        .HasColumnType("text");

                    b.Property<string>("ReceiversCC")
                        .HasColumnType("text");

                    b.Property<string>("ReceiversDirect")
                        .HasColumnType("text");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.ToTable("SmtpEmailProviders", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.NotificationProviders.WebhookProvider", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.NotificationProviders.NotificationProvider");

                    b.Property<string>("Headers")
                        .HasColumnType("text");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("text");

                    b.ToTable("WebhookProviders", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.ApplicationSettings", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.StatusPages.StatusPage", "DefaultStatusPage")
                        .WithMany()
                        .HasForeignKey("DefaultStatusPageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DefaultStatusPage");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Incidents.IncidentTimelineItem", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Incidents.Incident", "Incident")
                        .WithMany("Timeline")
                        .HasForeignKey("IncidentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Incident");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", "Monitor")
                        .WithMany("Rules")
                        .HasForeignKey("MonitorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Monitor");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Notifications.History.NotificationHistoryRecord", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", "Monitor")
                        .WithMany()
                        .HasForeignKey("MonitorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Monitor");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusHistory.StatusHistoryRecord", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", "Monitor")
                        .WithMany()
                        .HasForeignKey("MonitorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Monitor");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusPages.LabeledMonitor", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", "Monitor")
                        .WithMany()
                        .HasForeignKey("MonitorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenStatusPage.Server.Domain.Entities.StatusPages.MonitorSummary", "MonitorSummary")
                        .WithMany("LabeledMonitors")
                        .HasForeignKey("MonitorSummaryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Monitor");

                    b.Navigation("MonitorSummary");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusPages.MonitorSummary", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.StatusPages.StatusPage", "StatusPage")
                        .WithMany("MonitorSummaries")
                        .HasForeignKey("StatusPageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("StatusPage");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Persistence.Configurations.IncidentConfiguration+IncidentMonitorMapping", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Incidents.Incident", null)
                        .WithMany()
                        .HasForeignKey("IncidentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", null)
                        .WithMany()
                        .HasForeignKey("MonitorBaseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Persistence.Configurations.MonitorConfiguration+MonitorNotificationMapping", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", null)
                        .WithMany()
                        .HasForeignKey("MonitorBaseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenStatusPage.Server.Domain.Entities.NotificationProviders.NotificationProvider", null)
                        .WithMany()
                        .HasForeignKey("NotificationProviderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Dns.DnsMonitor", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Dns.DnsMonitor", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Dns.DnsRecordRule", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Dns.DnsRecordRule", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.HttpMonitor", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Http.HttpMonitor", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.ResponseBodyRule", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Http.ResponseBodyRule", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.ResponseHeaderRule", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Http.ResponseHeaderRule", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.SslCertificateRule", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Http.SslCertificateRule", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.StatusCodeRule", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Http.StatusCodeRule", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Ping.PingMonitor", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Ping.PingMonitor", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.ResponseTimeRule", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.ResponseTimeRule", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Ssh.SshCommandResultRule", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Ssh.SshCommandResultRule", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Ssh.SshMonitor", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Ssh.SshMonitor", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Tcp.TcpMonitor", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Tcp.TcpMonitor", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Udp.ResponseBytesRule", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Udp.ResponseBytesRule", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Udp.UdpMonitor", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.Monitors.Udp.UdpMonitor", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.NotificationProviders.SmtpEmailProvider", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.NotificationProviders.NotificationProvider", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.NotificationProviders.SmtpEmailProvider", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.NotificationProviders.WebhookProvider", b =>
                {
                    b.HasOne("OpenStatusPage.Server.Domain.Entities.NotificationProviders.NotificationProvider", null)
                        .WithOne()
                        .HasForeignKey("OpenStatusPage.Server.Domain.Entities.NotificationProviders.WebhookProvider", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Incidents.Incident", b =>
                {
                    b.Navigation("Timeline");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", b =>
                {
                    b.Navigation("Rules");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusPages.MonitorSummary", b =>
                {
                    b.Navigation("LabeledMonitors");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusPages.StatusPage", b =>
                {
                    b.Navigation("MonitorSummaries");
                });
#pragma warning restore 612, 618
        }
    }
}
