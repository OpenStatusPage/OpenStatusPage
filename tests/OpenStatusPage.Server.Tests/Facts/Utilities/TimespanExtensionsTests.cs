using OpenStatusPage.Shared.Utilities;
using System;
using Xunit;

namespace OpenStatusPage.Server.Tests.Facts.Utilities;

public class TimespanExtensionsTests
{
    [Fact]
    public void DurationString_TimespanZero_EmptyString()
    {
        // Assert
        Assert.Equal("", TimeSpan.Zero.DurationString());
    }

    [Fact]
    public void DurationString_OnEachSingle_FullSingleString()
    {
        // Arrange
        var timespan = TimeSpan.Zero;
        timespan = timespan.Add(TimeSpan.FromDays(1));
        timespan = timespan.Add(TimeSpan.FromHours(1));
        timespan = timespan.Add(TimeSpan.FromMinutes(1));
        timespan = timespan.Add(TimeSpan.FromSeconds(1));

        // Act
        var resultString = timespan.DurationString();

        // Assert
        Assert.Equal(" 1 day 1 hour 1 minute 1 second", resultString);
    }

    [Fact]
    public void DurationString_DifferentEach_FullPluralString()
    {
        // Arrange
        var timespan = TimeSpan.Zero;
        timespan = timespan.Add(TimeSpan.FromDays(1));
        timespan = timespan.Add(TimeSpan.FromHours(2));
        timespan = timespan.Add(TimeSpan.FromMinutes(3));
        timespan = timespan.Add(TimeSpan.FromSeconds(4));

        // Act
        var resultString = timespan.DurationString();

        // Assert
        Assert.Equal(" 1 day 2 hours 3 minutes 4 seconds", resultString);
    }
}
