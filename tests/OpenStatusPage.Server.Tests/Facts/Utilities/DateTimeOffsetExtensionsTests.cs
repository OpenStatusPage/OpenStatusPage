using OpenStatusPage.Shared.Utilities;
using System;
using Xunit;

namespace OpenStatusPage.Server.Tests.Facts.Utilities;

public class DateTimeOffsetExtensionsTests
{
    [Fact]
    public void IsInRangeInclusive_InRange_ReturnsTrue()
    {
        // Arrange
        var check = DateTimeOffset.UtcNow;
        var from = check.AddMinutes(-1);
        var until = check.AddMinutes(1);

        // Assert
        Assert.True(check.IsInRangeInclusive(from, until));
    }

    [Fact]
    public void IsInRangeInclusive_BeforeRange_ReturnsFalse()
    {
        // Arrange
        var check = DateTimeOffset.UtcNow;
        var from = check.AddMinutes(1);
        var until = check.AddMinutes(2);

        // Assert
        Assert.False(check.IsInRangeInclusive(from, until));
    }

    [Fact]
    public void IsInRangeInclusive_EndOverlap_ReturnsTrue()
    {
        // Arrange
        var check = DateTimeOffset.UtcNow;
        var from = check.AddMinutes(-1);
        var until = check;

        // Assert
        Assert.True(check.IsInRangeInclusive(from, until));
    }

    [Fact]
    public void IsInRangeExclusive_InRange_ReturnsTrue()
    {
        // Arrange
        var check = DateTimeOffset.UtcNow;
        var from = check.AddMinutes(-1);
        var until = check.AddMinutes(1);

        // Assert
        Assert.True(check.IsInRangeExclusive(from, until));
    }

    [Fact]
    public void IsInRangeExclusive_BeforeRange_ReturnsFalse()
    {
        // Arrange
        var check = DateTimeOffset.UtcNow;
        var from = check.AddMinutes(1);
        var until = check.AddMinutes(2);

        // Assert
        Assert.False(check.IsInRangeExclusive(from, until));
    }

    [Fact]
    public void IsInRangeExclusive_EndOverlap_ReturnsFalse()
    {
        // Arrange
        var check = DateTimeOffset.UtcNow;
        var from = check.AddMinutes(-1);
        var until = check;

        // Assert
        Assert.False(check.IsInRangeExclusive(from, until));
    }

    [Fact]
    public void IsInRangeInclusiveNullable_InRange_ReturnsTrue()
    {
        // Arrange
        var check = DateTimeOffset.UtcNow.AddYears(1);
        var from = check.AddMinutes(-1);

        // Assert
        Assert.True(check.IsInRangeInclusiveNullable(from, null));
    }

    [Fact]
    public void IsInRangeInclusiveNullable_BeforeRange_ReturnsFalse()
    {
        // Arrange
        var check = DateTimeOffset.UtcNow;
        var from = check.AddMinutes(1);

        // Assert
        Assert.False(check.IsInRangeInclusiveNullable(from, null));
    }

    [Fact]
    public void IsInRangeExclusiveNullable_InRange_ReturnsTrue()
    {
        // Arrange
        var check = DateTimeOffset.UtcNow.AddYears(1);
        var from = check.AddMinutes(-1);

        // Assert
        Assert.True(check.IsInRangeExclusiveNullable(from, null));
    }

    [Fact]
    public void IsInRangeExclusiveNullable_BeforeRange_ReturnsFalse()
    {
        // Arrange
        var check = DateTimeOffset.UtcNow;
        var from = check.AddMinutes(1);

        // Assert
        Assert.False(check.IsInRangeExclusiveNullable(from, null));
    }
}
