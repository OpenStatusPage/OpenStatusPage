using OpenStatusPage.Shared.Requests;
using Xunit;

namespace OpenStatusPage.Server.Tests.Facts.Requests;

public class SuccessResponseTests
{
    [Fact]
    public void SuccessResponse_FromSuccess_WasSuccessful()
    {
        // Act
        var respose = SuccessResponse.FromSuccess;

        // Assert
        Assert.True(respose.WasSuccessful);
    }

    [Fact]
    public void SuccessResponse_FromFailure_WasNotSuccessful()
    {
        // Act
        var respose = SuccessResponse.FromFailure;

        // Assert
        Assert.False(respose.WasSuccessful);
    }
}
