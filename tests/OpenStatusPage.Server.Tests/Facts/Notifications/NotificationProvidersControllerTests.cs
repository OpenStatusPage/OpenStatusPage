using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Notifications.Providers.Commands;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Tests.Helpers;
using OpenStatusPage.Shared.DataTransferObjects.NotificationProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Notifications;

public class NotificationProvidersControllerTests : TestBase, IDisposable
{
    protected readonly SingleMemberCluster _cluster;
    protected readonly ScopedMediatorExecutor _mediator;
    protected readonly HttpClient _httpClient;

    public NotificationProvidersControllerTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _cluster = SingleMemberCluster.CreateAsync(testOutput).GetAwaiter().GetResult();
        _mediator = _cluster.LeaderHost.Services.GetRequiredService<ScopedMediatorExecutor>();
        _httpClient = _cluster.LeaderHttpClient;
    }

    public void Dispose()
    {
        _cluster.Dispose();
    }

    public static NotificationProvider CreateNotificationProvider()
    {
        return new NotificationProvider()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "NotificationProvider",
            DefaultForNewMonitors = true,
            Enabled = true,
        };
    }

    public static WebhookProvider CreateWebhookProvider()
    {
        return new WebhookProvider()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "NotificationProvider",
            DefaultForNewMonitors = true,
            Enabled = true,
            Url = "https://webhook.site",
            Headers = "x-api-key=test"
        };
    }

    public static SmtpEmailProvider CreateSmtpEmailProvider()
    {
        return new SmtpEmailProvider()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "NotificationProvider",
            DefaultForNewMonitors = true,
            Enabled = true,
            Hostname = "smtp.example.org",
            Port = 487,
            Username = "admin@local.host",
            Password = "Password12345:)",
            DisplayName = "OpenStatusPageTesting",
            FromAddress = "tests@openstatuspage.local",
            ReceiversDirect = "void@mail.me;another@void.mail",
            ReceiversCC = "void2@mail.me",
            ReceiversBCC = "void3@mail.me",
        };
    }

    [Fact]
    public async Task GetAll_NoData_ResultEmptyAsync()
    {
        // Act
        var notificationProviders = await _httpClient.GetFromJsonAsync<List<NotificationProviderMetaDto>>("api/v1/NotificationProviders");

        // Assert
        Assert.Empty(notificationProviders);
    }

    [Fact]
    public async Task GetAll_DataExist_AllReturnedAsync()
    {
        // Arrange
        var provider = CreateNotificationProvider();

        await _mediator.Send(new CreateOrUpdateNotificationProviderCmd
        {
            Data = provider
        });

        // Act
        var notificationProviders = await _httpClient.GetFromJsonAsync<List<NotificationProviderMetaDto>>("api/v1/NotificationProviders");

        // Assert
        Assert.Single(notificationProviders);
        Assert.Equal(provider.Id, notificationProviders!.First().Id);
    }

    [Fact]
    public async Task GetDetails_NotExists_NotFoundAsync()
    {
        // Act
        var result = await _httpClient.GetAsync($"api/v1/NotificationProviders/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetDetails_WebhoookDataExist_DetailsReturnedAsync()
    {
        // Arrange
        var provider = CreateWebhookProvider();

        await _mediator.Send(new CreateOrUpdateNotificationProviderCmd
        {
            Data = provider
        });

        // Act
        var resultDto = await _httpClient.GetFromJsonAsync<WebhookProviderDto>($"api/v1/NotificationProviders/{provider.Id}?typename={nameof(WebhookProviderDto)}");

        // Assert
        Assert.NotNull(resultDto);
        Assert.Equal(provider.Id, resultDto.Id);
        Assert.Equal(provider.Name, resultDto.Name);
        Assert.Equal(provider.Url, resultDto.Url);
        Assert.Equal(provider.Headers, resultDto.Headers);
    }

    [Fact]
    public async Task Create_InvalidData_NotCreatedAsync()
    {
        // Arrange
        var monitor = CreateSmtpEmailProvider();
        monitor.Id = null!;

        var dto = _cluster.LeaderHost.Services.GetRequiredService<IMapper>().Map<SmtpEmailProviderDto>(monitor);

        // Act
        var respose = await _httpClient.PostAsJsonAsync($"api/v1/NotificationProviders?typename={nameof(SmtpEmailProviderDto)}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, respose.StatusCode);
    }

    [Fact]
    public async Task Update_SameVersionDifferentData_NoUpdateAsync()
    {
        // Arrange
        var provider = CreateSmtpEmailProvider();

        await _mediator.Send(new CreateOrUpdateNotificationProviderCmd
        {
            Data = provider
        });

        provider.Hostname = "CHANGED_DATA";

        // Act
        var dto = _cluster.LeaderHost.Services.GetRequiredService<IMapper>().Map<SmtpEmailProviderDto>(provider);

        var respose = await _httpClient.PostAsJsonAsync($"api/v1/NotificationProviders?typename={nameof(SmtpEmailProviderDto)}", dto);

        // Assert
        Assert.True(respose.IsSuccessStatusCode);

        var updatedProvider = (await _mediator.Send(new NotificationProvidersQuery())).NotificationProviders.First() as SmtpEmailProvider;

        Assert.NotNull(updatedProvider);
        Assert.Equal(provider.Id, updatedProvider.Id);
        Assert.NotEqual(provider.Hostname, updatedProvider.Hostname);
    }

    [Fact]
    public async Task Update_NewVersion_DataUpdatedAsync()
    {
        // Arrange
        var provider = CreateSmtpEmailProvider();

        await _mediator.Send(new CreateOrUpdateNotificationProviderCmd
        {
            Data = provider
        });

        provider.Version++;
        provider.Hostname = "CHANGED_DATA";

        // Act
        var dto = _cluster.LeaderHost.Services.GetRequiredService<IMapper>().Map<SmtpEmailProviderDto>(provider);

        var respose = await _httpClient.PostAsJsonAsync($"api/v1/NotificationProviders?typename={nameof(SmtpEmailProviderDto)}", dto);

        // Assert
        Assert.True(respose.IsSuccessStatusCode);

        var updatedProvider = (await _mediator.Send(new NotificationProvidersQuery())).NotificationProviders.First() as SmtpEmailProvider;

        Assert.NotNull(updatedProvider);
        Assert.Equal(provider.Id, updatedProvider.Id);
        Assert.Equal(provider.Hostname, updatedProvider.Hostname);
    }

    [Fact]
    public async Task Delete_Exist_DeletedAsync()
    {
        // Arrange
        var provider = CreateWebhookProvider();

        await _mediator.Send(new CreateOrUpdateNotificationProviderCmd
        {
            Data = provider
        });

        // Act
        var response = await _httpClient.DeleteAsync($"api/v1/NotificationProviders/{provider.Id}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Empty((await _mediator.Send(new NotificationProvidersQuery())).NotificationProviders);
    }
}
