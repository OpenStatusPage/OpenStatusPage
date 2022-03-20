using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Incidents.Commands;
using OpenStatusPage.Server.Application.Misc.Exceptions;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Server.Domain.Entities.Incidents;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Tests.Helpers;
using OpenStatusPage.Shared.DataTransferObjects.Incidents;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Requests.Incidents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Incidents;

public class IncidentsControllerTests : TestBase, IDisposable
{
    protected readonly SingleMemberCluster _cluster;
    protected readonly ScopedMediatorExecutor _mediator;
    protected readonly HttpClient _httpClient;

    public IncidentsControllerTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _cluster = SingleMemberCluster.CreateAsync(testOutput).GetAwaiter().GetResult();
        _mediator = _cluster.LeaderHost.Services.GetRequiredService<ScopedMediatorExecutor>();
        _httpClient = _cluster.LeaderHttpClient;
    }

    public void Dispose()
    {
        _cluster.Dispose();
    }

    public static Incident CreateIncident()
    {
        var incidentId = Guid.NewGuid().ToString();
        var incidentFrom = DateTimeOffset.UtcNow.AddDays(-1);

        return new Incident()
        {
            Id = incidentId,
            Name = "TestIncident",
            From = incidentFrom,
            Until = DateTimeOffset.UtcNow.AddHours(-1),
            AffectedServices = new List<MonitorBase>(),
            Timeline = new List<IncidentTimelineItem>()
            {
                new IncidentTimelineItem
                {
                    Id = Guid.NewGuid().ToString(),
                    IncidentId = incidentId,
                    DateTime = incidentFrom,
                    Severity = IncidentSeverity.Minor,
                    Status = IncidentStatus.Investigating,
                    AdditionalInformation = "AdditionalInformation"
                }
            }
        };
    }

    [Fact]
    public async Task GetAll_NoIncidents_ResultEmptyAsync()
    {
        // Act
        var incidents = await _httpClient.GetFromJsonAsync<List<IncidentMetaDto>>("api/v1/Incidents");

        // Assert
        Assert.Empty(incidents);
    }

    [Fact]
    public async Task GetAll_IncidentsExist_AllIncidentsReturnedAsync()
    {
        // Arrange
        var incident = CreateIncident();

        await _mediator.Send(new CreateOrUpdateIncidentCmd
        {
            Data = incident
        });

        // Act
        var incidents = await _httpClient.GetFromJsonAsync<List<IncidentMetaDto>>("api/v1/Incidents");

        // Assert
        Assert.Single(incidents);
        Assert.Equal(incident.Id, incidents!.First().Id);
    }

    [Fact]
    public async Task GetDetails_IncidentNotExists_NotFoundAsync()
    {
        // Act
        var result = await _httpClient.GetAsync($"api/v1/Incidents/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetDetails_IncidentExist_DetailsReturnedAsync()
    {
        // Arrange
        var incident = CreateIncident();

        await _mediator.Send(new CreateOrUpdateIncidentCmd
        {
            Data = incident
        });

        // Act
        var resultDto = await _httpClient.GetFromJsonAsync<IncidentDto>($"api/v1/Incidents/{incident.Id}");

        // Assert
        Assert.NotNull(resultDto);
        Assert.Equal(incident.Id, resultDto.Id);
        Assert.Equal(incident.Name, resultDto.Name);
        Assert.Equal(incident.From, resultDto.From);
        Assert.Equal(incident.AffectedServices.Count, resultDto.AffectedServices.Count);
        Assert.Equal(incident.Timeline.Count, resultDto.Timeline.Count);
    }

    [Fact]
    public async Task Create_InvalidData_NotCreatedAsync()
    {
        // Arrange
        var incident = CreateIncident();

        incident.Id = null!;

        // Assert
        await Assert.ThrowsAnyAsync<FinalFailureException>(() => _mediator.Send(new CreateOrUpdateIncidentCmd
        {
            Data = incident
        }));
    }

    [Fact]
    public async Task Update_IncidentExist_IncidentUpdatedAsync()
    {
        // Arrange
        var incident = CreateIncident();

        await _mediator.Send(new CreateOrUpdateIncidentCmd
        {
            Data = incident
        });

        incident.Version++;
        incident.Timeline.Add(new()
        {
            Id = Guid.NewGuid().ToString(),
            IncidentId = incident.Id,
            DateTime = DateTimeOffset.UtcNow,
            Severity = IncidentSeverity.Minor,
            Status = IncidentStatus.Resolved,
            AdditionalInformation = "Resolved"
        });

        // Act
        var dto = _cluster.LeaderHost.Services.GetRequiredService<IMapper>().Map<IncidentDto>(incident);

        var respose = await _httpClient.PostAsJsonAsync($"api/v1/Incidents", dto);

        // Assert
        var updatedIncident = (await _mediator.Send(new IncidentsQuery()
        {
            Query = new(query => query.Include(x => x.Timeline))
        })).Incidents.First();

        Assert.NotNull(updatedIncident);
        Assert.Equal(incident.Id, updatedIncident.Id);
        Assert.Equal(incident.Timeline.Count, updatedIncident.Timeline.Count);
        Assert.True(updatedIncident.Timeline.OrderBy(x => x.DateTime).Last().Status == IncidentStatus.Resolved);
    }

    [Fact]
    public async Task Delete_IncidentExist_IncidentDeletedAsync()
    {
        // Arrange
        var incident = CreateIncident();

        await _mediator.Send(new CreateOrUpdateIncidentCmd
        {
            Data = incident
        });

        // Act
        var response = await _httpClient.DeleteAsync($"api/v1/Incidents/{incident.Id}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Empty((await _mediator.Send(new IncidentsQuery())).Incidents);
    }

    [Fact]
    public async Task GetIncidentsForAffectedServices_NoIncidents_EmptyResponseAsync()
    {
        // Arrange
        var unauthenticatedHttpClient = new HttpClient { BaseAddress = _httpClient.BaseAddress };

        // Act
        var response = await unauthenticatedHttpClient.PostAsJsonAsync<IncidentsForServicesRequest>("api/v1/Incidents/public/bulk", new()
        {
            From = DateTimeOffset.UtcNow,
            Until = DateTimeOffset.UtcNow.AddDays(1),
            ServiceIds = new()
        });

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var responseData = await response.Content.ReadFromJsonAsync<IncidentsForServicesRequest.Response>();

        Assert.Empty(responseData.Incidents);
    }

    [Fact]
    public async Task GetIncidentsForAffectedServices_MatchingIncidents_AllReturnedAsync()
    {
        // Arrange
        var monitor = new MonitorBase()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "FakeMonitor",
            Tags = "fake",
            WorkerCount = 1,
            Interval = TimeSpan.FromSeconds(1),
            Rules = Array.Empty<MonitorRule>(),
            NotificationProviders = Array.Empty<NotificationProvider>(),
        };
        await _mediator.Send(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        var incident = CreateIncident();
        incident.AffectedServices.Add(monitor);

        await _mediator.Send(new CreateOrUpdateIncidentCmd
        {
            Data = incident
        });

        var unauthenticatedHttpClient = new HttpClient { BaseAddress = _httpClient.BaseAddress };

        // Act
        var response = await unauthenticatedHttpClient.PostAsJsonAsync<IncidentsForServicesRequest>("api/v1/Incidents/public/bulk", new()
        {
            From = DateTimeOffset.UtcNow.AddYears(-1),
            Until = DateTimeOffset.UtcNow.AddYears(1),
            ServiceIds = new() { monitor.Id }
        });

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var responseData = await response.Content.ReadFromJsonAsync<IncidentsForServicesRequest.Response>();

        Assert.Single(responseData.Incidents);
    }

    [Fact]
    public async Task GetIncidentsForAffectedServices_NoMatchingIncidents_NoReturnedAsync()
    {
        // Arrange
        var monitor = new MonitorBase()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "FakeMonitor",
            Tags = "fake",
            WorkerCount = 1,
            Interval = TimeSpan.FromSeconds(1),
            Rules = Array.Empty<MonitorRule>(),
            NotificationProviders = Array.Empty<NotificationProvider>(),
        };
        await _mediator.Send(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        var incident = CreateIncident();
        incident.AffectedServices.Add(monitor);

        await _mediator.Send(new CreateOrUpdateIncidentCmd
        {
            Data = incident
        });

        var unauthenticatedHttpClient = new HttpClient { BaseAddress = _httpClient.BaseAddress };

        // Act
        var response = await unauthenticatedHttpClient.PostAsJsonAsync<IncidentsForServicesRequest>("api/v1/Incidents/public/bulk", new()
        {
            From = DateTimeOffset.UtcNow.AddYears(-1),
            Until = DateTimeOffset.UtcNow.AddMonths(-11),
            ServiceIds = new() { monitor.Id }
        });

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var responseData = await response.Content.ReadFromJsonAsync<IncidentsForServicesRequest.Response>();

        Assert.Empty(responseData.Incidents);
    }
}
