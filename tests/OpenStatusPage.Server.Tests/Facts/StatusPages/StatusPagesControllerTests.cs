using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Server.Application.StatusPages.Commands;
using OpenStatusPage.Server.Domain.Entities.StatusPages;
using OpenStatusPage.Server.Tests.Facts.Monitors;
using OpenStatusPage.Server.Tests.Helpers;
using OpenStatusPage.Shared.DataTransferObjects.StatusPages;
using OpenStatusPage.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.StatusPages;

public class StatusPagesControllerTests : TestBase, IDisposable
{
    protected readonly SingleMemberCluster _cluster;
    protected readonly ScopedMediatorExecutor _mediator;
    protected readonly HttpClient _httpClient;

    public StatusPagesControllerTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _cluster = SingleMemberCluster.CreateAsync(testOutput).GetAwaiter().GetResult();
        _mediator = _cluster.LeaderHost.Services.GetRequiredService<ScopedMediatorExecutor>();
        _httpClient = _cluster.LeaderHttpClient;
    }

    public void Dispose()
    {
        _cluster.Dispose();
    }

    public static StatusPage CreateStatusPage()
    {
        var statusPageId = Guid.NewGuid().ToString();

        return new StatusPage()
        {
            Id = statusPageId,
            Name = "StatusPage",
            Password = "Password",
            Description = "# Markdown\nMultiline content",
            EnableGlobalSummary = true,
            EnableUpcomingMaintenances = true,
            DaysUpcomingMaintenances = 14,
            DaysStatusHistory = 14,
            EnableIncidentTimeline = true,
            DaysIncidentTimeline = 14,
            MonitorSummaries = new List<MonitorSummary>()
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    StatusPageId = statusPageId,
                    OrderIndex = 0,
                    Title = "My first summary",
                    ShowHistory = false,
                    LabeledMonitors = new List<LabeledMonitor>()
                }
            }
        };
    }

    [Fact]
    public async Task GetAll_DataExist_AllReturnedAsync()
    {
        // Act
        var statusPages = await _httpClient.GetFromJsonAsync<List<StatusPageMetaDto>>("api/v1/StatusPages");

        // Assert
        Assert.Single(statusPages);
    }

    [Fact]
    public async Task GetDetails_NotExists_NotFoundAsync()
    {
        // Act
        var result = await _httpClient.GetAsync($"api/v1/StatusPages/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetDetails_DataExist_DetailsReturnedAsync()
    {
        // Arrange
        var monitor = MonitorsControllerTests.CreateMonitorBase();
        await _mediator.Send(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        var statusPage = CreateStatusPage();

        statusPage.MonitorSummaries.First().LabeledMonitors.Add(new()
        {
            Id = Guid.NewGuid().ToString(),
            Label = "Alias name for monitor",
            MonitorId = monitor.Id,
            MonitorSummaryId = statusPage.MonitorSummaries.First().Id,
            OrderIndex = 0
        });

        await _mediator.Send(new CreateOrUpdateStatusPageCmd
        {
            Data = statusPage
        });

        // Act
        var resultDto = await _httpClient.GetFromJsonAsync<StatusPageConfigurationDto>($"api/v1/StatusPages/{statusPage.Id}");

        // Assert
        Assert.NotNull(resultDto);
        Assert.Equal(statusPage.Id, resultDto.Id);
        Assert.Equal(statusPage.Name, resultDto.Name);
        Assert.Equal(statusPage.Description, resultDto.Description);
        Assert.Equal(statusPage.EnableGlobalSummary, resultDto.EnableGlobalSummary);
        Assert.Equal(statusPage.EnableUpcomingMaintenances, resultDto.EnableUpcomingMaintenances);
        Assert.Equal(statusPage.DaysUpcomingMaintenances, resultDto.DaysUpcomingMaintenances);
        Assert.Equal(statusPage.DaysStatusHistory, resultDto.DaysStatusHistory);
        Assert.Equal(statusPage.EnableIncidentTimeline, resultDto.EnableIncidentTimeline);
        Assert.Equal(statusPage.DaysIncidentTimeline, resultDto.DaysIncidentTimeline);
        Assert.Equal(statusPage.MonitorSummaries.Count, resultDto.MonitorSummaries.Count);
    }

    [Fact]
    public async Task Create_InvalidData_NotCreatedAsync()
    {
        // Arrange
        var statusPage = CreateStatusPage();

        await _mediator.Send(new CreateOrUpdateStatusPageCmd
        {
            Data = statusPage
        });

        statusPage.Id = null!;

        // Act
        var dto = _cluster.LeaderHost.Services.GetRequiredService<IMapper>().Map<StatusPageConfigurationDto>(statusPage);
        var respose = await _httpClient.PostAsJsonAsync($"api/v1/StatusPages", dto);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, respose.StatusCode);
    }

    [Fact]
    public async Task Update_NewVersion_DataUpdatedAsync()
    {
        // Arrange
        var statusPage = CreateStatusPage();

        await _mediator.Send(new CreateOrUpdateStatusPageCmd
        {
            Data = statusPage
        });

        statusPage.Version++;
        statusPage.Name = "NewName";

        // Act
        var dto = _cluster.LeaderHost.Services.GetRequiredService<IMapper>().Map<StatusPageConfigurationDto>(statusPage);
        var respose = await _httpClient.PostAsJsonAsync($"api/v1/StatusPages", dto);

        // Assert
        Assert.True(respose.IsSuccessStatusCode);

        var updatedStatusPage = (await _mediator.Send(new StatusPagesQuery()
        {
            Query = new(query => query
                .Where(x => x.Id == statusPage.Id)
                .Include(x => x.MonitorSummaries)
                .ThenInclude(x => x.LabeledMonitors))
        })).StatusPages.First();

        Assert.NotNull(updatedStatusPage);
        Assert.Equal(statusPage.Id, updatedStatusPage.Id);
        Assert.Equal(statusPage.Name, updatedStatusPage.Name);
        Assert.Equal(statusPage.Description, updatedStatusPage.Description);
        Assert.Equal(statusPage.EnableGlobalSummary, updatedStatusPage.EnableGlobalSummary);
        Assert.Equal(statusPage.EnableUpcomingMaintenances, updatedStatusPage.EnableUpcomingMaintenances);
        Assert.Equal(statusPage.DaysUpcomingMaintenances, updatedStatusPage.DaysUpcomingMaintenances);
        Assert.Equal(statusPage.DaysStatusHistory, updatedStatusPage.DaysStatusHistory);
        Assert.Equal(statusPage.EnableIncidentTimeline, updatedStatusPage.EnableIncidentTimeline);
        Assert.Equal(statusPage.DaysIncidentTimeline, updatedStatusPage.DaysIncidentTimeline);
        Assert.Equal(statusPage.MonitorSummaries.Count, updatedStatusPage.MonitorSummaries.Count);
    }

    [Fact]
    public async Task Delete_Exist_DeletedAsync()
    {
        // Arrange
        var statusPage = CreateStatusPage();

        await _mediator.Send(new CreateOrUpdateStatusPageCmd
        {
            Data = statusPage
        });

        // Act
        var response = await _httpClient.DeleteAsync($"api/v1/StatusPages/{statusPage.Id}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Empty((await _mediator.Send(new StatusPagesQuery()
        {
            Query = new(query => query.Where(x => x.Id == statusPage.Id))
        })).StatusPages);
    }

    [Fact]
    public async Task GetStatusPagePublic_NoSearch_BadRequestAsync()
    {
        // Arrange
        var unauthenticatedHttpClient = new HttpClient { BaseAddress = _httpClient.BaseAddress };

        // Act
        var response = await unauthenticatedHttpClient.GetAsync($"api/v1/StatusPages/public");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetStatusPagePublic_PublicStatusPageSearchDefault_ReturnedAsync()
    {
        // Arrange
        var unauthenticatedHttpClient = new HttpClient { BaseAddress = _httpClient.BaseAddress };

        // Act
        var resultDto = await unauthenticatedHttpClient.GetFromJsonAsync<StatusPageConfigurationDto>($"api/v1/StatusPages/public/default");

        // Assert
        Assert.NotNull(resultDto);
    }

    [Fact]
    public async Task GetStatusPagePublic_PrivateStatusPageSearchIdNoAuth_UnauthorizedAsync()
    {
        // Arrange
        var statusPage = CreateStatusPage();

        await _mediator.Send(new CreateOrUpdateStatusPageCmd
        {
            Data = statusPage
        });

        var unauthenticatedHttpClient = new HttpClient { BaseAddress = _httpClient.BaseAddress };

        // Act
        var response = await unauthenticatedHttpClient.GetAsync($"api/v1/StatusPages/public/{statusPage.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStatusPagePublic_PrivateStatusPageSearchNameWithAuth_ReturnedAsync()
    {
        // Arrange
        var statusPage = CreateStatusPage();

        await _mediator.Send(new CreateOrUpdateStatusPageCmd
        {
            Data = statusPage
        });

        var authenticatedHttpClient = new HttpClient { BaseAddress = _httpClient.BaseAddress };
        authenticatedHttpClient.DefaultRequestHeaders.Add("X-StatusPage-Access-Token", SHA256Hash.Create(statusPage.Password!));

        // Act
        var resultDto = await authenticatedHttpClient.GetFromJsonAsync<StatusPageConfigurationDto>($"api/v1/StatusPages/public/{statusPage.Name}");

        // Assert
        Assert.NotNull(resultDto);
    }
}
