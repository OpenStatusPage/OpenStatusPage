using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Server.Application.StatusHistory.Commands;
using OpenStatusPage.Server.Tests.Facts.Monitors;
using OpenStatusPage.Server.Tests.Helpers;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Requests.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
namespace OpenStatusPage.Server.Tests.Facts.StatusHistory;

public class ServiceStatusHistoriesControllerTests : TestBase, IDisposable
{
    protected readonly SingleMemberCluster _cluster;
    protected readonly ScopedMediatorExecutor _mediator;
    protected readonly HttpClient _httpClient;

    public ServiceStatusHistoriesControllerTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _cluster = SingleMemberCluster.CreateAsync(testOutput).GetAwaiter().GetResult();
        _mediator = _cluster.LeaderHost.Services.GetRequiredService<ScopedMediatorExecutor>();
        _httpClient = _cluster.LeaderHttpClient;
    }

    public void Dispose()
    {
        _cluster.Dispose();
    }

    [Fact]
    public async Task GetStatusHistoryForServices_NoData_EmptyResponseAsync()
    {
        // Arrange
        var unauthenticatedHttpClient = new HttpClient { BaseAddress = _httpClient.BaseAddress };

        // Act
        var response = await unauthenticatedHttpClient.PostAsJsonAsync<ServiceStatusHistoryRequest>("api/v1/ServiceStatusHistories/public/bulk", new()
        {
            From = DateTimeOffset.UtcNow,
            Until = DateTimeOffset.UtcNow.AddDays(1),
            ServiceIds = new List<string>()
        });

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var responseData = await response.Content.ReadFromJsonAsync<ServiceStatusHistoryRequest.Response>();

        Assert.Empty(responseData.ServiceStatusHistories);
    }

    [Fact]
    public async Task GetStatusHistoryForServices_MatchingData_DataReturnedAsync()
    {
        // Arrange
        var monitor = MonitorsControllerTests.CreateMonitorBase();

        await _mediator.Send(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        await _mediator.Send(new CreateStatusHistoryRecordCmd
        {
            MonitorId = monitor.Id,
            Status = ServiceStatus.Unavailable,
            UtcFrom = DateTimeOffset.UtcNow.AddDays(-7).UtcDateTime
        });

        await _mediator.Send(new CreateStatusHistoryRecordCmd
        {
            MonitorId = monitor.Id,
            Status = ServiceStatus.Degraded,
            UtcFrom = DateTimeOffset.UtcNow.AddDays(-6).UtcDateTime
        });

        await _mediator.Send(new CreateStatusHistoryRecordCmd
        {
            MonitorId = monitor.Id,
            Status = ServiceStatus.Unknown,
            UtcFrom = DateTimeOffset.UtcNow.AddDays(-5).UtcDateTime
        });

        await _mediator.Send(new CreateStatusHistoryRecordCmd
        {
            MonitorId = monitor.Id,
            Status = ServiceStatus.Available,
            UtcFrom = DateTimeOffset.UtcNow.AddDays(-4).UtcDateTime
        });

        var unauthenticatedHttpClient = new HttpClient { BaseAddress = _httpClient.BaseAddress };

        // Act
        var response = await unauthenticatedHttpClient.PostAsJsonAsync<ServiceStatusHistoryRequest>("api/v1/ServiceStatusHistories/public/bulk", new()
        {
            From = DateTimeOffset.UtcNow.AddDays(-14),
            Until = DateTimeOffset.UtcNow.AddDays(1),
            ServiceIds = new List<string>()
            {
                monitor.Id,
            }
        });

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var responseData = await response.Content.ReadFromJsonAsync<ServiceStatusHistoryRequest.Response>();

        Assert.Equal(2, responseData.ServiceStatusHistories.Count);
        Assert.Equal(2, responseData.ServiceStatusHistories.First().Outages.Count);
        Assert.Empty(responseData.ServiceStatusHistories.Last().Outages);
    }

    [Fact]
    public async Task GetStatusHistoryForServices_NoMatchingData_EmptyResultAsync()
    {
        // Arrange
        var monitor = MonitorsControllerTests.CreateMonitorBase();

        await _mediator.Send(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        await _mediator.Send(new CreateStatusHistoryRecordCmd
        {
            MonitorId = monitor.Id,
            Status = ServiceStatus.Unavailable,
            UtcFrom = DateTimeOffset.UtcNow.AddDays(5).UtcDateTime
        });

        var unauthenticatedHttpClient = new HttpClient { BaseAddress = _httpClient.BaseAddress };

        // Act
        var response = await unauthenticatedHttpClient.PostAsJsonAsync<ServiceStatusHistoryRequest>("api/v1/ServiceStatusHistories/public/bulk", new()
        {
            From = DateTimeOffset.UtcNow,
            Until = DateTimeOffset.UtcNow.AddDays(1),
            ServiceIds = new List<string>()
            {
                monitor.Id,
            }
        });

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var responseData = await response.Content.ReadFromJsonAsync<ServiceStatusHistoryRequest.Response>();

        Assert.Empty(responseData.ServiceStatusHistories);
    }
}
