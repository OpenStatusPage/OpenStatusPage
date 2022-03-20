using OpenStatusPage.Server.Tests.Helpers;
using OpenStatusPage.Shared.DataTransferObjects.Configuration;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Configuration;

public class ApplicationSettingsControllerTests : TestBase
{
    public ApplicationSettingsControllerTests(ITestOutputHelper testOutput) : base(testOutput)
    {
    }

    [Fact]
    public async Task PostData_ValidData_SuccessfulGetResultAsync()
    {
        // Arrange
        var cluster = await SingleMemberCluster.CreateAsync(_testOutput);
        var req = (await cluster.LeaderHttpClient.GetAsync("api/v1/ApplicationSettings"));
        var data = await req.Content.ReadFromJsonAsync<ApplicationSettingsDto>();

        data.Version++;
        data.StatusFlushInterval = TimeSpan.FromHours(42);
        data.DaysMonitorHistory = 42;
        data.DaysIncidentHistory = 42;

        // Act
        var postResult = await cluster.LeaderHttpClient.PostAsJsonAsync("api/v1/ApplicationSettings", data);

        // Assert
        Assert.True(postResult.IsSuccessStatusCode);

        var updatedData = await (await cluster.LeaderHttpClient.GetAsync("api/v1/ApplicationSettings")).Content.ReadFromJsonAsync<ApplicationSettingsDto>();

        Assert.Equal(updatedData.Version, data.Version);
        Assert.Equal(updatedData.StatusFlushInterval, data.StatusFlushInterval);
        Assert.Equal(updatedData.DaysMonitorHistory, data.DaysMonitorHistory);
        Assert.Equal(updatedData.DaysIncidentHistory, data.DaysIncidentHistory);
    }

    [Fact]
    public async Task PostData_EmptyDto_BadRequestAsync()
    {
        // Arrange
        var cluster = await SingleMemberCluster.CreateAsync(_testOutput);

        // Act
        var postResult = await cluster.LeaderHttpClient.PostAsJsonAsync("api/v1/ApplicationSettings", new ApplicationSettingsDto());

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, postResult.StatusCode);
    }

    [Fact]
    public async Task PostData_InvalidData_ProblemAsync()
    {
        // Arrange
        var cluster = await SingleMemberCluster.CreateAsync(_testOutput);

        var data = new ApplicationSettingsDto
        {
            Id = "I DO NOT EXIST",
            Version = -1,
            StatusFlushInterval = TimeSpan.FromHours(-100000),
            DaysMonitorHistory = 10000,
            DaysIncidentHistory = 10000,
            DefaultStatusPageId = "I DO NOT EXIST"
        };

        // Act
        var postResult = await cluster.LeaderHttpClient.PostAsJsonAsync("api/v1/ApplicationSettings", data);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, postResult.StatusCode);
    }
}
