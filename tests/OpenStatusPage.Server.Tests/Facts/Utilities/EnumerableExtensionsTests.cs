using OpenStatusPage.Shared.Utilities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OpenStatusPage.Server.Tests.Facts.Utilities;

public class EnumerableExtensionsTests
{
    [Fact]
    public void PairwiseLeadingDefault_Empty_ReturnsEmpty()
    {
        // Arrange
        var list = new List<object>();

        // Act
        var pairWiseEnumerable = list.PairwiseLeadingDefault().ToList();

        // Assert
        Assert.Empty(pairWiseEnumerable);
    }

    [Fact]
    public void PairwiseLeadingDefault_SingleEntry_LeadingDefault()
    {
        // Arrange
        var list = new List<object>()
        {
            new object(),
        };

        // Act
        var pairWiseEnumerable = list.PairwiseLeadingDefault().ToList();

        // Assert
        Assert.Single(pairWiseEnumerable);
        Assert.Null(pairWiseEnumerable.First().Item1);
        Assert.NotNull(pairWiseEnumerable.First().Item2);
    }

    [Fact]
    public void PairwiseLeadingDefault_TwoEntries_DefaultFirstFullSecond()
    {
        // Arrange
        var list = new List<object>()
        {
            new object(),
            new object(),
        };

        // Act
        var pairWiseEnumerable = list.PairwiseLeadingDefault().ToList();

        // Assert
        Assert.Equal(2, pairWiseEnumerable.Count);
        Assert.Null(pairWiseEnumerable.First().Item1);
        Assert.NotNull(pairWiseEnumerable.First().Item2);
        Assert.NotNull(pairWiseEnumerable.Last().Item1);
        Assert.NotNull(pairWiseEnumerable.Last().Item2);
    }
}