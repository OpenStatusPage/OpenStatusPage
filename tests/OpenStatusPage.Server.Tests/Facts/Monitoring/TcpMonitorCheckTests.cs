using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Tcp;
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

public class TcpMonitorCheckTests : TestBase
{
    protected ILogger _voidLogger;

    public TcpMonitorCheckTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _voidLogger = new NullLoggerFactory().CreateLogger("voidlogger");
    }

    public static TcpMonitor CreateTpcMonitor()
    {
        var monitorId = Guid.NewGuid().ToString();

        return new TcpMonitor()
        {
            Id = monitorId,
            Enabled = false,
            Name = "TcpMonitor",
            Tags = "testing",
            WorkerCount = 1,
            Interval = TimeSpan.FromHours(1),
            Timeout = TimeSpan.FromMinutes(1),
            Rules = new List<MonitorRule>(),
            NotificationProviders = Array.Empty<NotificationProvider>()
        };
    }

    [Fact]
    public async Task DoCheck_ResponseTimeExceeded_ReturnsDegradedAsync()
    {
        // Arrange
        var monitor = CreateTpcMonitor();
        monitor.Hostname = "openstatus.page";
        monitor.Port = 443;
        monitor.Rules.Add(new ResponseTimeRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 0,
            ViolationStatus = ServiceStatus.Degraded,
            ComparisonType = NumericComparisonType.GreaterThan,
            ComparisonValue = 0,
        });

        // Act
        var result = await new TcpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Degraded, result.Item2);
    }

    [Fact]
    public async Task DoCheck_ResponseTimeRespected_ReturnsAvailableAsync()
    {
        // Arrange
        var monitor = CreateTpcMonitor();
        monitor.Hostname = "openstatus.page";
        monitor.Port = 443;
        monitor.Rules.Add(new ResponseTimeRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 0,
            ViolationStatus = ServiceStatus.Degraded,
            ComparisonType = NumericComparisonType.GreaterThan,
            ComparisonValue = 10000,
        });

        // Act
        var result = await new TcpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Available, result.Item2);
    }

    [Fact]
    public async Task DoCheck_InvalidHost_ReturnsUnavailableAsync()
    {
        // Arrange
        var monitor = CreateTpcMonitor();
        monitor.Hostname = "i-do-not-exit.openstatus.page";
        monitor.Port = 4242;

        // Act
        var result = await new TcpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Unavailable, result.Item2);
    }
}
