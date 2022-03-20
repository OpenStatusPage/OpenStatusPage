using OpenStatusPage.Server.Tests.Helpers;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Authentication;

public class ApiKeyAuthenticationHandlerTests : TestBase
{
    public ApiKeyAuthenticationHandlerTests(ITestOutputHelper testOutput) : base(testOutput)
    {
    }

    [Fact]
    public async Task AttemptAuthorization_NoApiKey_UnauthorizedAsync()
    {
        // Arrange
        var cluster = await SingleMemberCluster.CreateAsync(_testOutput);
        var unauthenticatedHttpClient = new HttpClient { BaseAddress = cluster.LeaderHttpClient.BaseAddress };

        // Act
        var requestResult = await unauthenticatedHttpClient.GetAsync("api/v1/404");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, requestResult.StatusCode);
    }

    [Fact]
    public async Task AttemptAuthorization_ValidApiKey_UnauthorizedAsync()
    {
        // Arrange
        var cluster = await SingleMemberCluster.CreateAsync(_testOutput);

        // Act
        var requestResult = await cluster.LeaderHttpClient.GetAsync("api/v1/404");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, requestResult.StatusCode);
    }
}
