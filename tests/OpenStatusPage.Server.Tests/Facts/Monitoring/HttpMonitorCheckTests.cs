using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Http;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Tests.Helpers;
using OpenStatusPage.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Monitoring;

public class HttpMonitorCheckTests : TestBase
{
    protected ILogger _voidLogger;

    public HttpMonitorCheckTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _voidLogger = new NullLoggerFactory().CreateLogger("voidlogger");
    }

    public static HttpMonitor CreateHttpMonitor()
    {
        var monitorId = Guid.NewGuid().ToString();

        return new HttpMonitor()
        {
            Id = monitorId,
            Enabled = false,
            Name = "HttpMonitor",
            Tags = "testing",
            Url = "http://example.org",
            Method = HttpVerb.GET,
            MaxRedirects = 5,
            WorkerCount = 1,
            Interval = TimeSpan.FromHours(1),
            Rules = new List<MonitorRule>(),
            NotificationProviders = Array.Empty<NotificationProvider>()
        };
    }

    [Fact]
    public async Task DoCheck_ResponseTimeExceeded_ReturnsDegradedAsync()
    {
        // Arrange
        using var testHttpServer = WebApplication.Create(new[] { "--Logging:LogLevel:Default=None" });
        testHttpServer.MapGet("/", async (context) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            context.Response.StatusCode = 200;
        });
        _ = testHttpServer.RunAsync("http://127.0.0.1:0");

        var monitor = CreateHttpMonitor();
        monitor.Url = testHttpServer.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
        monitor.Rules.Add(new ResponseTimeRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 0,
            ViolationStatus = ServiceStatus.Degraded,
            ComparisonType = NumericComparisonType.GreaterThan,
            ComparisonValue = 100,
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Degraded, result.Item2);
    }

    [Fact]
    public async Task DoCheck_ResponseTimeRespected_ReturnsAvailableAsync()
    {
        // Arrange
        using var testHttpServer = WebApplication.Create(new[] { "--Logging:LogLevel:Default=None" });
        testHttpServer.MapGet("/", async (context) =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10));
            context.Response.StatusCode = 200;
        });
        _ = testHttpServer.RunAsync("http://127.0.0.1:0");

        var monitor = CreateHttpMonitor();
        monitor.Url = testHttpServer.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
        monitor.Rules.Add(new ResponseTimeRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 0,
            ViolationStatus = ServiceStatus.Degraded,
            ComparisonType = NumericComparisonType.GreaterThan,
            ComparisonValue = 100,
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Available, result.Item2);
    }

    [Fact]
    public async Task DoCheck_ResponseHeaderFound_ReturnsAvailableAsync()
    {
        // Arrange
        using var testHttpServer = WebApplication.Create(new[] { "--Logging:LogLevel:Default=None" });
        testHttpServer.MapGet("/", async (context) =>
        {
            context.Response.Headers.Add("X-Custom-Header", "HEADER_VALUE");
            context.Response.StatusCode = 200;
        });
        _ = testHttpServer.RunAsync("http://127.0.0.1:0");

        var monitor = CreateHttpMonitor();
        monitor.Url = testHttpServer.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
        monitor.Rules.Add(new ResponseHeaderRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 1,
            ViolationStatus = ServiceStatus.Unavailable,
            ComparisonType = StringComparisonType.NotContains,
            ComparisonValue = "HEADER_VALUE",
            Key = "X-Custom-Header"
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Available, result.Item2);
    }

    [Fact]
    public async Task DoCheck_ResponseHeaderNoFound_ReturnsUnavailableAsync()
    {
        // Arrange
        using var testHttpServer = WebApplication.Create(new[] { "--Logging:LogLevel:Default=None" });
        testHttpServer.MapGet("/", async (context) =>
        {
            context.Response.StatusCode = 200;
        });
        _ = testHttpServer.RunAsync("http://127.0.0.1:0");

        var monitor = CreateHttpMonitor();
        monitor.Url = testHttpServer.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
        monitor.Rules.Add(new ResponseHeaderRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 1,
            ViolationStatus = ServiceStatus.Unavailable,
            ComparisonType = StringComparisonType.NotContains,
            ComparisonValue = "HEADER_VALUE",
            Key = "X-Custom-Header"
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Available, result.Item2);
    }

    [Fact]
    public async Task DoCheck_ResponseBodyKeywordFound_ReturnsAvailableAsync()
    {
        // Arrange
        using var testHttpServer = WebApplication.Create(new[] { "--Logging:LogLevel:Default=None" });
        testHttpServer.MapGet("/", () => "Hello World with SomeKeyword :)");
        _ = testHttpServer.RunAsync("http://127.0.0.1:0");

        var monitor = CreateHttpMonitor();
        monitor.Url = testHttpServer.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
        monitor.Rules.Add(new ResponseBodyRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 2,
            ViolationStatus = ServiceStatus.Degraded,
            ComparisonType = StringComparisonType.NotContains,
            ComparisonValue = "SomeKeyword"
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Available, result.Item2);
    }

    [Fact]
    public async Task DoCheck_ResponseBodyKeywordNotFound_ReturnsDegradedAsync()
    {
        // Arrange
        using var testHttpServer = WebApplication.Create(new[] { "--Logging:LogLevel:Default=None" });
        testHttpServer.MapGet("/", () => "Hello World :)");
        _ = testHttpServer.RunAsync("http://127.0.0.1:0");

        var monitor = CreateHttpMonitor();
        monitor.Url = testHttpServer.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
        monitor.Rules.Add(new ResponseBodyRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 2,
            ViolationStatus = ServiceStatus.Degraded,
            ComparisonType = StringComparisonType.NotContains,
            ComparisonValue = "SomeKeyword"
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Degraded, result.Item2);
    }

    [Fact]
    public async Task DoCheck_StatusCodeExact_ReturnsAvailableAsync()
    {
        // Arrange
        using var testHttpServer = WebApplication.Create(new[] { "--Logging:LogLevel:Default=None" });
        testHttpServer.MapGet("/", async (context) =>
        {
            context.Response.StatusCode = 204;
        });
        _ = testHttpServer.RunAsync("http://127.0.0.1:0");

        var monitor = CreateHttpMonitor();
        monitor.Url = testHttpServer.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
        monitor.Rules.Add(new StatusCodeRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 4,
            ViolationStatus = ServiceStatus.Unavailable,
            Value = 204
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Available, result.Item2);
    }

    [Fact]
    public async Task DoCheck_StatusCodeRange_ReturnsAvailableAsync()
    {
        // Arrange
        using var testHttpServer = WebApplication.Create(new[] { "--Logging:LogLevel:Default=None" });
        testHttpServer.MapGet("/", async (context) =>
        {
            context.Response.StatusCode = 204;
        });
        _ = testHttpServer.RunAsync("http://127.0.0.1:0");

        var monitor = CreateHttpMonitor();
        monitor.Url = testHttpServer.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
        monitor.Rules.Add(new StatusCodeRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 4,
            ViolationStatus = ServiceStatus.Unavailable,
            Value = 200,
            UpperRangeValue = 299
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Unavailable, result.Item2);
    }

    [Fact]
    public async Task DoCheck_StatusCodeRangeViolated_ReturnsUnavailableAsync()
    {
        // Arrange
        using var testHttpServer = WebApplication.Create(new[] { "--Logging:LogLevel:Default=None" });
        testHttpServer.MapGet("/", async (context) =>
        {
            context.Response.StatusCode = 404;
        });
        _ = testHttpServer.RunAsync("http://127.0.0.1:0");

        var monitor = CreateHttpMonitor();
        monitor.Url = testHttpServer.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
        monitor.Rules.Add(new StatusCodeRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 4,
            ViolationStatus = ServiceStatus.Unavailable,
            Value = 200,
            UpperRangeValue = 299
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Unavailable, result.Item2);
    }

    [Fact]
    public async Task DoCheck_NoSSL_ReturnsUnavailableAsync()
    {
        // Arrange
        using var testHttpServer = WebApplication.Create(new[] { "--Logging:LogLevel:Default=None" });
        testHttpServer.MapGet("/", async (context) =>
        {
            context.Response.StatusCode = 404;
        });
        _ = testHttpServer.RunAsync("http://127.0.0.1:0");

        var monitor = CreateHttpMonitor();
        monitor.Url = testHttpServer.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
        monitor.Rules.Add(new SslCertificateRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 3,
            ViolationStatus = ServiceStatus.Unavailable,
            CheckType = SslCertificateCheckType.NotExists
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Unavailable, result.Item2);
    }

    [Fact]
    public async Task DoCheck_InvalidSSL_ReturnsDegradedAsync()
    {
        // Arrange
        var monitor = CreateHttpMonitor();
        monitor.Url = "https://expired.badssl.com";
        monitor.Rules.Add(new SslCertificateRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 3,
            ViolationStatus = ServiceStatus.Degraded,
            CheckType = SslCertificateCheckType.NotValid
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Degraded, result.Item2);
    }

    [Fact]
    public async Task DoCheck_SSLValidDaysExeeeded_ReturnsDegradedAsync()
    {
        // Arrange
        var monitor = CreateHttpMonitor();
        monitor.Url = "https://openstatus.page";
        monitor.Rules.Add(new SslCertificateRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 3,
            ViolationStatus = ServiceStatus.Degraded,
            CheckType = SslCertificateCheckType.NotValid,
            MinValidTimespan = TimeSpan.FromDays(3560),
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Degraded, result.Item2);
    }

    [Fact]
    public async Task DoCheck_SSLValidDaysok_ReturnsAvailableAsync()
    {
        // Arrange
        var monitor = CreateHttpMonitor();
        monitor.Url = "https://openstatus.page";
        monitor.Rules.Add(new SslCertificateRule()
        {
            Id = Guid.NewGuid().ToString(),
            MonitorId = monitor.Id,
            OrderIndex = 3,
            ViolationStatus = ServiceStatus.Degraded,
            CheckType = SslCertificateCheckType.NotValid,
            MinValidTimespan = TimeSpan.FromDays(1),
        });

        // Act
        var result = await new HttpMonitorCheck().PerformAsync(monitor, DateTimeOffset.UtcNow, ServiceStatus.Available, _voidLogger, new CancellationTokenSource().Token);

        // Assert
        Assert.Equal(ServiceStatus.Available, result.Item2);
    }
}
