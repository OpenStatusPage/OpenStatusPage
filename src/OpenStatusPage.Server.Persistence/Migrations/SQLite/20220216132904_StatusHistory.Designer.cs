﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenStatusPage.Server.Persistence.Drivers;

#nullable disable

namespace OpenStatusPage.Server.Persistence.Migrations.SQLite
{
    [DbContext(typeof(SQLiteDbContext))]
    [Migration("20220216132904_StatusHistory")]
    partial class StatusHistory
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.1");

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.ApplicationSettings", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<ushort>("DaysIncidentHistory")
                        .HasColumnType("INTEGER");

                    b.Property<ushort>("DaysMonitorHistory")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DefaultStatusPageId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("DefaultStatusPageId");

                    b.ToTable("ApplicationSettings");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Cluster.ClusterMember", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("Endpoint")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ClusterMembers");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Cluster.RaftLogMetaEntry", b =>
                {
                    b.Property<long>("Index")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Term")
                        .HasColumnType("INTEGER");

                    b.HasKey("Index");

                    b.ToTable("RaftLogMetaEntries");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Incidents.Incident", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("From")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("Until")
                        .HasColumnType("TEXT");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Incidents");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Incidents.IncidentTimelineItem", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("AdditionalInformation")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("DateTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("IncidentId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Severity")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("IncidentId");

                    b.ToTable("IncidentTimelineItems", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Enabled")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan>("Interval")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ushort?>("Retries")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan?>("RetryInterval")
                        .HasColumnType("TEXT");

                    b.Property<string>("Tags")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan?>("Timeout")
                        .HasColumnType("TEXT");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.Property<int>("WorkerCount")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Monitors");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("MonitorId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ushort>("OrderIndex")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ViolationStatus")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("MonitorId");

                    b.ToTable("MonitorRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.NotificationProviders.NotificationProvider", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<bool>("DefaultForNewMonitors")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Enabled")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("NotificationProviders");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusHistory.StatusHistoryRecord", b =>
                {
                    b.Property<string>("MonitorId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("FromUtc")
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.HasKey("MonitorId", "FromUtc");

                    b.ToTable("StatusHistoryRecords");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusPages.LabeledMonitor", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("MonitorId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("MonitorSummaryId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("OrderIndex")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("MonitorId");

                    b.HasIndex("MonitorSummaryId");

                    b.ToTable("LabeledMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusPages.MonitorSummary", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("OrderIndex")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowHistory")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StatusPageId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("StatusPageId");

                    b.ToTable("MonitorSummaries", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.StatusPages.StatusPage", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int?>("DaysIncidentTimeline")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DaysStatusHistory")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DaysUpcomingMaintenances")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<bool>("EnableGlobalSummary")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("EnableIncidentTimeline")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("EnableUpcomingMaintenances")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .HasColumnType("TEXT");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("StatusPages");
                });

            modelBuilder.Entity("OpenStatusPage.Server.Persistence.Configurations.IncidentConfiguration+IncidentMonitorMapping", b =>
                {
                    b.Property<string>("MonitorBaseId")
                        .HasColumnType("TEXT");

                    b.Property<string>("IncidentId")
                        .HasColumnType("TEXT");

                    b.HasKey("MonitorBaseId", "IncidentId");

                    b.HasIndex("IncidentId");

                    b.ToTable("IncidentMonitorMappings", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Persistence.Configurations.MonitorConfiguration+MonitorNotificationMapping", b =>
                {
                    b.Property<string>("MonitorBaseId")
                        .HasColumnType("TEXT");

                    b.Property<string>("NotificationProviderId")
                        .HasColumnType("TEXT");

                    b.HasKey("MonitorBaseId", "NotificationProviderId");

                    b.HasIndex("NotificationProviderId");

                    b.ToTable("MonitorNotificationMappings", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Dns.DnsMonitor", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("RecordType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Resolvers")
                        .HasColumnType("TEXT");

                    b.ToTable("DnsMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Dns.DnsRecordRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("ComparisonType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ComparisonValue")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.ToTable("DnsRecordRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.HttpMonitor", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase");

                    b.Property<string>("AuthenticationAdditional")
                        .HasColumnType("TEXT");

                    b.Property<string>("AuthenticationBase")
                        .HasColumnType("TEXT");

                    b.Property<int>("AuthenticationScheme")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Body")
                        .HasColumnType("TEXT");

                    b.Property<string>("Headers")
                        .HasColumnType("TEXT");

                    b.Property<ushort>("MaxRedirects")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Method")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.ToTable("HttpMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.ResponseBodyRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("ComparisonType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ComparisonValue")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.ToTable("ResponseBodyRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.ResponseHeaderRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("ComparisonType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ComparisonValue")
                        .HasColumnType("TEXT");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.ToTable("ResponseHeaderRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.SslCertificateRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("CheckType")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan?>("MinValidTimespan")
                        .HasColumnType("TEXT");

                    b.ToTable("SslCertificateRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Http.StatusCodeRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<ushort?>("UpperRangeValue")
                        .HasColumnType("INTEGER");

                    b.Property<ushort>("Value")
                        .HasColumnType("INTEGER");

                    b.ToTable("StatusCodeRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Ping.PingMonitor", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.ToTable("PingMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.ResponseTimeRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("ComparisonType")
                        .HasColumnType("INTEGER");

                    b.Property<ushort>("ComparisonValue")
                        .HasColumnType("INTEGER");

                    b.ToTable("ResponseTimeRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Ssh.SshCommandResultRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("ComparisonType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ComparisonValue")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.ToTable("SshCommandResultRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Ssh.SshMonitor", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase");

                    b.Property<string>("Command")
                        .HasColumnType("TEXT");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .HasColumnType("TEXT");

                    b.Property<ushort?>("Port")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PrivateKey")
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.ToTable("SshMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Tcp.TcpMonitor", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ushort>("Port")
                        .HasColumnType("INTEGER");

                    b.ToTable("TcpMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Udp.ResponseBytesRule", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorRule");

                    b.Property<int>("ComparisonType")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("ExpectedBytes")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.ToTable("ResponseBytesRules", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.Monitors.Udp.UdpMonitor", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.Monitors.MonitorBase");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ushort>("Port")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("RequestBytes")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.ToTable("UdpMonitors", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.NotificationProviders.SmtpEmailProvider", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.NotificationProviders.NotificationProvider");

                    b.Property<string>("FromAddress")
                        .HasColumnType("TEXT");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ushort?>("Port")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ReceiversBCC")
                        .HasColumnType("TEXT");

                    b.Property<string>("ReceiversCC")
                        .HasColumnType("TEXT");

                    b.Property<string>("ReceiversDirect")
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.ToTable("SmtpEmailProviders", (string)null);
                });

            modelBuilder.Entity("OpenStatusPage.Server.Domain.Entities.NotificationProviders.WebhookProvider", b =>
                {
                    b.HasBaseType("OpenStatusPage.Server.Domain.Entities.NotificationProviders.NotificationProvider");

                    b.Property<string>("Headers")
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("TEXT");

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
