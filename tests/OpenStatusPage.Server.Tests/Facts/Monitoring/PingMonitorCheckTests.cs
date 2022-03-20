using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ping;
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

public class PingMonitorCheckTests : TestBase
{
    protected ILogger _voidLogger;

    public PingMonitorCheckTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _voidLogger = new NullLoggerFactory().CreateLogger("voidlogger");
    }

    public static PingMonitor CreatePingMonitor()
    {
        var monitorId = Guid.NewGuid().ToString();

        return new PingMonitor()
        {
            Id = monitorId,
            Enabled = true,
            Name = "PingMonitor",
            Tags = "testing",
            Hostname = "localhost",
            WorkerCount = 1,
            Interval = TimeSpan.FromHours(1),
            Rules = new List<MonitorRule>(),
            NotificationProviders = Array.Empty<NotificationProvider>()
        };
    }

    [Fact]
    public async Task DoCheck_ReachableHost_ReturnsAvailableAsync()
    {
        // Arrange
        var monitor = CreatePingMonitor();

        // Act
        var result = await new PingMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Available, result.Item2);
    }

    [Fact]
    public async Task DoCheck_ReachableHostInternet_ReturnsDegradedAsync()
    {
        // Arrange
        var monitor = CreatePingMonitor();
        monitor.Hostname = "openstatus.page";
        monitor.Rules.Add(new ResponseTimeRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 0,
            ViolationStatus = ServiceStatus.Degraded,
            ComparisonType = NumericComparisonType.GreaterThan,
            ComparisonValue = 1,
        });

        // Act
        var result = await new PingMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Degraded, result.Item2);
    }

    [Fact]
    public async Task DoCheck_InvalidHost_ReturnsUnavailableAsync()
    {
        // Arrange
        var monitor = CreatePingMonitor();
        monitor.Hostname = "dead.domain.tld";

        // Act
        var result = await new PingMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Unavailable, result.Item2);
    }
}
