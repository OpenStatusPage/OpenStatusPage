using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ssh;
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

public class SshMonitorCheckTests : TestBase
{
    protected ILogger _voidLogger;

    public SshMonitorCheckTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _voidLogger = new NullLoggerFactory().CreateLogger("voidlogger");
    }

    public static SshMonitor CreateSshMonitor()
    {
        var monitorId = Guid.NewGuid().ToString();

        return new SshMonitor()
        {
            Id = monitorId,
            Enabled = false,
            Name = "SshMonitor",
            Tags = "",
            Hostname = "127.0.0.1",
            Port = 4242,
            Username = "root",
            Password = "password",
            Command = "ls",
            WorkerCount = 1,
            Interval = TimeSpan.FromDays(1),
            Rules = new List<MonitorRule>()
            {
                new ResponseTimeRule()
                {
                    Id = Guid.NewGuid().ToString(),
                    MonitorId = monitorId,
                    OrderIndex = 0,
                    ViolationStatus = ServiceStatus.Unavailable,
                    ComparisonType = NumericComparisonType.GreaterThan,
                    ComparisonValue = 42,
                },
                new SshCommandResultRule()
                {
                    Id = Guid.NewGuid().ToString(),
                    MonitorId = monitorId,
                    OrderIndex = 1,
                    ViolationStatus = ServiceStatus.Degraded,
                    ComparisonType = StringComparisonType.NotContains,
                    ComparisonValue = "helloworld.txt"
                }
            },
            NotificationProviders = Array.Empty<NotificationProvider>()
        };
    }

    [Fact]
    public async Task DoCheck_InvalidHost_ReturnsUnavailableAsync()
    {
        // Arrange
        var monitor = CreateSshMonitor();
        monitor.Hostname = "dead.domain.tld";

        // Act
        var result = await new SshMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Unavailable, result.Item2);
    }
}
