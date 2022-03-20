using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Dns;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Tests.Helpers;
using OpenStatusPage.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Monitoring;

public class DnsMonitorCheckTests : TestBase
{
    protected ILogger _voidLogger;

    public DnsMonitorCheckTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _voidLogger = new NullLoggerFactory().CreateLogger("voidlogger");
    }

    public static DnsMonitor CreateDnsMonitor()
    {
        var monitorId = Guid.NewGuid().ToString();

        return new DnsMonitor()
        {
            Id = monitorId,
            Enabled = true,
            Hostname = "openstatus.page",
            RecordType = DnsRecordType.A,
            Name = "DnsMonitor",
            Tags = "testing",
            Interval = TimeSpan.FromHours(1),
            Retries = 1,
            RetryInterval = TimeSpan.FromSeconds(5),
            Timeout = TimeSpan.FromSeconds(5),
            WorkerCount = 1,
            Rules = new List<MonitorRule>(),
            NotificationProviders = Array.Empty<NotificationProvider>()
        };
    }

    [Fact]
    public async Task DoCheck_ValidRecord_ReturnsAvailableAsync()
    {
        // Arrange
        var monitor = CreateDnsMonitor();
        monitor.Resolvers = "1.1.1.1\n8.8.8.8;8.8.4.4";

        // Act
        var result = await new DnsMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Available, result.Item2);
    }

    [Fact]
    public async Task DoCheck_RecordWithImpossibleRule_ReturnsDegradedAsync()
    {
        // Arrange
        var monitor = CreateDnsMonitor();
        monitor.RecordType = DnsRecordType.MX;
        monitor.Rules.Add(new DnsRecordRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 0,
            ViolationStatus = ServiceStatus.Degraded,
            ComparisonType = StringComparisonType.NotContains,
            ComparisonValue = "IAmNotInTheRecords"
        });

        // Act
        var result = await new DnsMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Degraded, result.Item2);
    }

    [Fact]
    public async Task DoCheck_NoRecords_ReturnsUnavailableAsync()
    {
        // Arrange
        var monitor = CreateDnsMonitor();
        monitor.Hostname = "invalid.domain.tld";
        monitor.RecordType = DnsRecordType.AAAA;

        // Act
        var result = await new DnsMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Unavailable, result.Item2);
    }
}
