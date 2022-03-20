using Bunit;
using OpenStatusPage.Client.Pages._Components;
using Xunit;

using static Bunit.ComponentParameterFactory;

namespace OpenStatusPage.Client.Tests;

public class DataLoaderTests
{
    [Fact]
    public void RenderDataLoader_NullDataNoContent_SpinnerShown()
    {
        // Arrange
        using var ctx = new TestContext();

        // Act
        var cut = ctx.RenderComponent<DataLoader>(
          (nameof(DataLoader.WaitFor), null)
        );

        var renderedMarkup = cut.Markup;

        var spinner = cut.Find("circle");

        // Assert
        Assert.True(spinner.ClassList.Contains("mud-progress-circular-circle"));
    }

    [Fact]
    public void RenderDataLoader_ValidDataNoContent_EmptyOutput()
    {
        // Arrange
        using var ctx = new TestContext();

        // Act
        var cut = ctx.RenderComponent<DataLoader>(
          (nameof(DataLoader.WaitFor), new object())
        );

        var renderedMarkup = cut.Markup;

        // Assert
        Assert.Equal("", renderedMarkup);
    }

    [Fact]
    public void RenderDataLoader_ValidDataChildContent_ChildContentOutput()
    {
        // Arrange
        using var ctx = new TestContext();

        // Act
        var cut = ctx.RenderComponent<DataLoader>(
          (nameof(DataLoader.WaitFor), new object()),
          ChildContent("<b>Hello ChildContent</b>")
        );

        var renderedMarkup = cut.Markup;

        // Assert
        Assert.Equal("<b>Hello ChildContent</b>", renderedMarkup);
    }
}
