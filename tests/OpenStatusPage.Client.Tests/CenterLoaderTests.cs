using Bunit;
using OpenStatusPage.Client.Pages._Components;
using Xunit;

namespace OpenStatusPage.Client.Tests;

public class CenterLoaderTests
{
    [Fact]
    public void RenderCenterLoader_SpinnerShown()
    {
        // Arrange
        using var ctx = new TestContext();

        // Act
        var component = ctx.RenderComponent<CenterLoader>();

        // Assert
        var renderedMarkup = component.Markup;

        var spinner = component.Find("circle");

        Assert.True(spinner.ClassList.Contains("mud-progress-circular-circle"));
    }
}
