using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Http;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ping;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ssh;
using OpenStatusPage.Server.Domain.Entities.Monitors.Udp;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Tests.Helpers;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Http;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Ping;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Udp;
using OpenStatusPage.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Monitors;

public class MonitorsControllerTests : TestBase, IDisposable
{
    protected readonly SingleMemberCluster _cluster;
    protected readonly ScopedMediatorExecutor _mediator;
    protected readonly HttpClient _httpClient;

    public MonitorsControllerTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _cluster = SingleMemberCluster.CreateAsync(testOutput).GetAwaiter().GetResult();
        _mediator = _cluster.LeaderHost.Services.GetRequiredService<ScopedMediatorExecutor>();
        _httpClient = _cluster.LeaderHttpClient;
    }

    public void Dispose()
    {
        _cluster.Dispose();
    }

    public static MonitorBase CreateMonitorBase()
    {
        var monitorId = Guid.NewGuid().ToString();

        return new MonitorBase()
        {
            Id = monitorId,
            Enabled = false,
            Name = "MonitorBase",
            Tags = "testing",
            Interval = TimeSpan.FromHours(1),
            Retries = 1,
            RetryInterval = TimeSpan.FromSeconds(1),
            Timeout = TimeSpan.FromSeconds(1),
            WorkerCount = 3,
            Rules = new List<MonitorRule>()
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    MonitorId = monitorId,
                    OrderIndex = 0,
                    ViolationStatus = ServiceStatus.Unavailable
                }
            },
            NotificationProviders = Array.Empty<NotificationProvider>()
        };
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
                new ResponseHeaderRule()
                {
                    Id = Guid.NewGuid().ToString(),
                    MonitorId = monitorId,
                    OrderIndex = 1,
                    ViolationStatus = ServiceStatus.Degraded,
                    ComparisonType = StringComparisonType.NotContains,
                    ComparisonValue = "HEADER_VALUE",
                    Key = "X-Custom-Header"
                },
                new ResponseBodyRule()
                {
                    Id = Guid.NewGuid().ToString(),
                    MonitorId = monitorId,
                    OrderIndex = 2,
                    ViolationStatus = ServiceStatus.Degraded,
                    ComparisonType = StringComparisonType.NotContains,
                    ComparisonValue = "SomeKeyword"
                },
                new SslCertificateRule()
                {
                    Id = Guid.NewGuid().ToString(),
                    MonitorId = monitorId,
                    OrderIndex = 3,
                    ViolationStatus = ServiceStatus.Degraded,
                    CheckType = SslCertificateCheckType.NotValid,
                    MinValidTimespan = TimeSpan.FromDays(7),
                },
                new StatusCodeRule()
                {
                    Id = Guid.NewGuid().ToString(),
                    MonitorId = monitorId,
                    OrderIndex = 4,
                    ViolationStatus = ServiceStatus.Degraded,
                    Value = 200,
                    UpperRangeValue = 299
                }
            },
            NotificationProviders = Array.Empty<NotificationProvider>()
        };
    }

    public static PingMonitor CreatePingMonitor()
    {
        var monitorId = Guid.NewGuid().ToString();

        return new PingMonitor()
        {
            Id = monitorId,
            Enabled = false,
            Name = "PingMonitor",
            Tags = "testing",
            Hostname = "localhost",
            WorkerCount = 1,
            Interval = TimeSpan.FromHours(1),
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
                }
            },
            NotificationProviders = Array.Empty<NotificationProvider>()
        };
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
            Hostname = "localhost",
            Port = 1337,
            RequestBytes = new byte[] { 0xFF, 0x1C },
            WorkerCount = 1,
            Interval = TimeSpan.FromHours(1),
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
                new ResponseBytesRule()
                {
                    Id = Guid.NewGuid().ToString(),
                    MonitorId = monitorId,
                    OrderIndex = 1,
                    ViolationStatus = ServiceStatus.Unavailable,
                    ComparisonType = BytesComparisonType.NotEqual,
                    ExpectedBytes = new byte[]{ 0xFF, 0x1C },
                }
            },
            NotificationProviders = Array.Empty<NotificationProvider>()
        };
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
            Username = "root",
            Password = "password",
            PrivateKey = "OPENSSHKEY",
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
    public async Task GetAll_NoData_ResultEmptyAsync()
    {
        // Act
        var monitors = await _httpClient.GetFromJsonAsync<List<MonitorMetaDto>>("api/v1/Monitors");

        // Assert
        Assert.Empty(monitors);
    }

    [Fact]
    public async Task GetAll_DataExist_AllReturnedAsync()
    {
        // Arrange
        var monitor = CreateMonitorBase();

        await _mediator.Send(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        // Act
        var monitors = await _httpClient.GetFromJsonAsync<List<MonitorMetaDto>>("api/v1/Monitors");

        // Assert
        Assert.Single(monitors);
        Assert.Equal(monitor.Id, monitors!.First().Id);
    }


    [Fact]
    public async Task GetDetails_NotExists_NotFoundAsync()
    {
        // Act
        var result = await _httpClient.GetAsync($"api/v1/Monitors/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetDetails_BaseDataExist_BaseDetailsReturnedAsync()
    {
        // Arrange
        var monitor = CreateMonitorBase();

        await _mediator.Send(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        // Act
        var resultDto = await _httpClient.GetFromJsonAsync<MonitorDto>($"api/v1/Monitors/{monitor.Id}");

        // Assert
        Assert.NotNull(resultDto);
        Assert.Equal(monitor.Id, resultDto.Id);
        Assert.Equal(monitor.Version, resultDto.Version);
        Assert.Equal(monitor.Enabled, resultDto.Enabled);
        Assert.Equal(monitor.Name, resultDto.Name);
        Assert.Equal(monitor.Interval, resultDto.Interval);
        Assert.Equal(monitor.NotificationProviders.Count, resultDto.NotificationProviderMetas.Count);
    }

    [Fact]
    public async Task GetDetails_HttpMonitorExist_HttpDetailsReturnedAsync()
    {
        // Arrange
        var monitor = CreateHttpMonitor();

        await _mediator.Send(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        // Act
        var resultDto = await _httpClient.GetFromJsonAsync<HttpMonitorDto>($"api/v1/Monitors/{monitor.Id}?typename={nameof(HttpMonitorDto)}");

        // Assert
        Assert.NotNull(resultDto);
        Assert.Equal(monitor.Id, resultDto.Id);
        Assert.Equal(monitor.Version, resultDto.Version);
        Assert.Equal(monitor.Enabled, resultDto.Enabled);
        Assert.Equal(monitor.Name, resultDto.Name);
        Assert.Equal(monitor.Interval, resultDto.Interval);
        Assert.Equal(monitor.NotificationProviders.Count, resultDto.NotificationProviderMetas.Count);
        Assert.Single(resultDto.ResponseTimeRules);
        Assert.Single(resultDto.ResponseHeaderRules);
        Assert.Single(resultDto.ResponseBodyRules);
        Assert.Single(resultDto.SslCertificateRules);
        Assert.Single(resultDto.StatusCodeRules);
    }

    [Fact]
    public async Task GetDetails_UdpMonitorExist_UdpDetailsReturnedAsync()
    {
        // Arrange
        var monitor = CreateUdpMonitor();

        await _mediator.Send(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        // Act
        var resultDto = await _httpClient.GetFromJsonAsync<UdpMonitorDto>($"api/v1/Monitors/{monitor.Id}?typename={nameof(UdpMonitorDto)}");

        // Assert
        Assert.NotNull(resultDto);
        Assert.Equal(monitor.Id, resultDto.Id);
        Assert.Equal(monitor.Version, resultDto.Version);
        Assert.Equal(monitor.Enabled, resultDto.Enabled);
        Assert.Equal(monitor.Name, resultDto.Name);
        Assert.Equal(monitor.Interval, resultDto.Interval);
        Assert.Equal(monitor.NotificationProviders.Count, resultDto.NotificationProviderMetas.Count);
        Assert.Single(resultDto.ResponseTimeRules);
        Assert.Single(resultDto.ResponseBytesRules);
    }

    [Fact]
    public async Task Create_InvalidData_NotCreatedAsync()
    {
        // Arrange
        var monitor = CreateHttpMonitor();
        monitor.Id = null!;

        var dto = _cluster.LeaderHost.Services.GetRequiredService<IMapper>().Map<HttpMonitorDto>(monitor);

        // Act
        var respose = await _httpClient.PostAsJsonAsync($"api/v1/Monitors?typename={nameof(HttpMonitorDto)}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, respose.StatusCode);
    }

    [Fact]
    public async Task Update_SameVersionDifferentData_NoUpdateAsync()
    {
        // Arrange
        var monitor = CreatePingMonitor();

        await _mediator.Send(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        monitor.Hostname = "CHANGED_DATA";

        // Act
        var dto = _cluster.LeaderHost.Services.GetRequiredService<IMapper>().Map<PingMonitorDto>(monitor);

        var respose = await _httpClient.PostAsJsonAsync($"api/v1/Monitors?typename={nameof(PingMonitorDto)}", dto);

        // Assert
        Assert.True(respose.IsSuccessStatusCode);

        var updatedMonitor = (await _mediator.Send(new MonitorsQuery()
        {
            Query = new(query => query
                .Include(x => x.Rules)
                .Include(x => x.NotificationProviders))
        })).Monitors.First() as PingMonitor;

        Assert.NotNull(updatedMonitor);
        Assert.Equal(monitor.Id, updatedMonitor.Id);
        Assert.NotEqual(monitor.Hostname, updatedMonitor.Hostname);
    }

    [Fact]
    public async Task Update_NewVersion_DataUpdatedAsync()
    {
        // Arrange
        var monitor = CreateUdpMonitor();

        await _mediator.Send(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        monitor.Version++;
        monitor.Hostname = "127.0.0.1";

        // Act
        var dto = _cluster.LeaderHost.Services.GetRequiredService<IMapper>().Map<UdpMonitorDto>(monitor);

        var respose = await _httpClient.PostAsJsonAsync($"api/v1/Monitors?typename={nameof(UdpMonitorDto)}", dto);

        // Assert
        Assert.True(respose.IsSuccessStatusCode);

        var updatedMonitor = (await _mediator.Send(new MonitorsQuery()
        {
            Query = new(query => query
                .Include(x => x.Rules)
                .Include(x => x.NotificationProviders))
        })).Monitors.First() as UdpMonitor;

        Assert.NotNull(updatedMonitor);
        Assert.Equal(monitor.Id, updatedMonitor.Id);
        Assert.Equal(monitor.Hostname, updatedMonitor.Hostname);
        Assert.Equal(monitor.RequestBytes, updatedMonitor.RequestBytes);
    }

    [Fact]
    public async Task Delete_Exist_DeletedAsync()
    {
        // Arrange
        var monitor = CreateMonitorBase();

        await _mediator.Send(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        // Act
        var response = await _httpClient.DeleteAsync($"api/v1/Monitors/{monitor.Id}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Empty((await _mediator.Send(new MonitorsQuery())).Monitors);
    }
}
