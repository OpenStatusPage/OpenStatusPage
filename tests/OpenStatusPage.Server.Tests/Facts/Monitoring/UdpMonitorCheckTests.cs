using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Udp;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Tests.Helpers;
using OpenStatusPage.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Monitoring;

public class UdpMonitorCheckTests : TestBase, IDisposable
{
    protected ILogger _voidLogger;
    protected bool _serverActive;
    protected UdpClient _udpEchoServer;

    public UdpMonitorCheckTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _voidLogger = new NullLoggerFactory().CreateLogger("voidlogger");

        _serverActive = true;
        _udpEchoServer = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));

        //Console.WriteLine($">>>>>> UdpMonitorCheckTests Started on: {_udpEchoServer.Client.LocalEndPoint as IPEndPoint}");

        _ = Task.Run(async () =>
        {
            while (_serverActive)
            {
                try
                {
                    var remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = _udpEchoServer.Receive(ref remoteIPEndPoint);

                    //Console.WriteLine($">>>>>> UdpMonitorCheckTests Received bytes from: {remoteIPEndPoint}");

                    await Task.Delay(1000);

                    if (receivedBytes != null && !(receivedBytes.Length > 0 && receivedBytes[0] == 0xCC))
                    {
                        _udpEchoServer.Send(receivedBytes, receivedBytes.Length, remoteIPEndPoint);
                    }
                }
                catch// (Exception ex)
                {
                    //Console.WriteLine($">>>>>> UdpMonitorCheckTests Exception: {ex.Message}\n{ex.StackTrace}");
                }
            }
        });
    }

    public void Dispose()
    {
        try
        {
            _serverActive = false;
            _udpEchoServer.Close();
            _udpEchoServer.Dispose();
            _udpEchoServer = null!;
        }
        catch// (Exception ex)
        {
            //Console.WriteLine($">>>>>> Dispose Exception: {ex.Message}");
        }
    }

    public static UdpMonitor CreateUdpMonitor()
    {
        var monitorId = Guid.NewGuid().ToString();

        return new UdpMonitor()
        {
            Id = monitorId,
            Enabled = false,
            Name = "UdpMonitor",
            Tags = "testing",
            Hostname = "127.0.0.1",
            RequestBytes = Array.Empty<byte>(),
            WorkerCount = 1,
            Interval = TimeSpan.FromHours(1),
            Timeout = TimeSpan.FromSeconds(5),
            Rules = new List<MonitorRule>(),
            NotificationProviders = Array.Empty<NotificationProvider>()
        };
    }

    [Fact]
    public async Task DoCheck_EmptyRequestBytesSent_ReturnsAvailableAsync()
    {
        // Arrange
        var monitor = CreateUdpMonitor();
        monitor.Port = (ushort)(_udpEchoServer.Client.LocalEndPoint as IPEndPoint).Port;

        // Act
        var result = await new UdpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Available, result.Item2);
    }

    [Fact]
    public async Task DoCheck_RequestBytesSent_ReturnsDegradedAfterTimeAsync()
    {
        // Arrange
        var monitor = CreateUdpMonitor();
        monitor.Port = (ushort)(_udpEchoServer.Client.LocalEndPoint as IPEndPoint).Port;
        monitor.RequestBytes = new byte[] { 0xFF, 0x1C };
        monitor.Rules.Add(new ResponseTimeRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 0,
            ViolationStatus = ServiceStatus.Degraded,
            ComparisonType = NumericComparisonType.GreaterThan,
            ComparisonValue = 10,
        });

        // Act
        var result = await new UdpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Degraded, result.Item2);
    }

    [Fact]
    public async Task DoCheck_RequestBytesSent_ReturnsAvailableAsync()
    {
        // Arrange
        var monitor = CreateUdpMonitor();
        monitor.Port = (ushort)(_udpEchoServer.Client.LocalEndPoint as IPEndPoint).Port;
        monitor.RequestBytes = new byte[] { 0xFF, 0x1C };
        monitor.Rules.Add(new ResponseBytesRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 0,
            ViolationStatus = ServiceStatus.Degraded,
            ComparisonType = BytesComparisonType.Equal,
            ExpectedBytes = new byte[] { 0xFF, 0x1C },
        });

        // Act
        var result = await new UdpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Available, result.Item2);
    }

    [Fact]
    public async Task DoCheck_EndEndpoint_ReturnsUnavailableAsync()
    {
        // Arrange
        var monitor = CreateUdpMonitor();
        monitor.Port = (ushort)(_udpEchoServer.Client.LocalEndPoint as IPEndPoint).Port;
        monitor.RequestBytes = new byte[] { 0xCC }; //0xCC indicates no response

        // Act
        var result = await new UdpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Unavailable, result.Item2);
    }
}
