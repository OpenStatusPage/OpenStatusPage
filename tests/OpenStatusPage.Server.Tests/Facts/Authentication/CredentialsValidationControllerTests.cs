using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.StatusPages.Commands;
using OpenStatusPage.Server.Tests.Helpers;
using OpenStatusPage.Shared.Requests.Credentials;
using OpenStatusPage.Shared.Utilities;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Authentication;

public class CredentialsValidationControllerTests : TestBase
{
    public CredentialsValidationControllerTests(ITestOutputHelper testOutput) : base(testOutput)
    {
    }

    [Fact]
    public async Task ValidateCredentials_ValidCredentials_AllCredentialsReturnedAsync()
    {
        // Arrange
        using var cluster = await SingleMemberCluster.CreateAsync(_testOutput);
        var leaderSettings = cluster.LeaderHost.Services.GetRequiredService<EnvironmentSettings>();
        var leaderMember = cluster.LeaderHost.Services.GetRequiredService<ClusterService>().GetLocalMember();

        var leaderMediator = cluster.LeaderHost.Services.GetRequiredService<ScopedMediatorExecutor>();
        var defaultStatusPage = (await leaderMediator.Send(new StatusPagesQuery()).WaitAsync(_testWaitMax)).StatusPages.First();
        defaultStatusPage.Version++;
        defaultStatusPage.Password = "TestPassword";
        await leaderMediator.Send(new CreateOrUpdateStatusPageCmd
        {
            Data = defaultStatusPage
        });

        var unauthenticatedHttpClient = new HttpClient { BaseAddress = cluster.LeaderHttpClient.BaseAddress };

        // Act
        var response = await unauthenticatedHttpClient.PostAsJsonAsync<CredentialsValidationRequest>("auth/v1/CredentialsValidation", new()
        {
            DashboardCredentials = new()
            {
                ApiKey = leaderSettings.ApiKey,
                ConnectEndpoint = leaderMember.Endpoint.ToString()
            },
            StatusPageCredentials = new()
            {
                new()
                {
                    StatusPageId = defaultStatusPage.Id,
                    PasswordHash = SHA256Hash.Create(defaultStatusPage.Password)
                }
            }
        });

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var responseData = await response.Content.ReadFromJsonAsync<CredentialsValidationRequest.Response>();

        Assert.NotNull(responseData.ValidDashboardCredentials);
        Assert.Single(responseData.ValidStatusPageCredentials);
    }

    [Fact]
    public async Task ValidateCredentials_InvalidCredentials_NoCredentialsReturnedAsync()
    {
        // Arrange
        using var cluster = await SingleMemberCluster.CreateAsync(_testOutput);
        var leaderSettings = cluster.LeaderHost.Services.GetRequiredService<EnvironmentSettings>();
        var leaderMember = cluster.LeaderHost.Services.GetRequiredService<ClusterService>().GetLocalMember();

        var leaderMediator = cluster.LeaderHost.Services.GetRequiredService<ScopedMediatorExecutor>();
        var defaultStatusPage = (await leaderMediator.Send(new StatusPagesQuery()).WaitAsync(_testWaitMax)).StatusPages.First();
        defaultStatusPage.Version++;
        defaultStatusPage.Password = "TestPassword";
        await leaderMediator.Send(new CreateOrUpdateStatusPageCmd
        {
            Data = defaultStatusPage
        });

        var unauthenticatedHttpClient = new HttpClient { BaseAddress = cluster.LeaderHttpClient.BaseAddress };

        // Act
        var response = await unauthenticatedHttpClient.PostAsJsonAsync<CredentialsValidationRequest>("auth/v1/CredentialsValidation", new()
        {
            DashboardCredentials = new()
            {
                ApiKey = "IncorrectApiKey",
                ConnectEndpoint = "IncorrectEndpoint"
            },
            StatusPageCredentials = new()
            {
                new()
                {
                    StatusPageId = defaultStatusPage.Id,
                    PasswordHash = SHA256Hash.Create("IncorrectPassword")
                }
            }
        });

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var responseData = await response.Content.ReadFromJsonAsync<CredentialsValidationRequest.Response>();

        Assert.Null(responseData.ValidDashboardCredentials);
        Assert.Empty(responseData.ValidStatusPageCredentials);
    }
}
