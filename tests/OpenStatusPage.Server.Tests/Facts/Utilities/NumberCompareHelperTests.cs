using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Utilities;
using Xunit;

namespace OpenStatusPage.Server.Tests.Facts.Utilities;

public class NumberCompareHelperTests
{
    [Fact]
    public void TestAll()
    {
        // Assert
        Assert.True(NumberCompareHelper.Compare(42, NumericComparisonType.Equal, 42));
        Assert.False(NumberCompareHelper.Compare(42, NumericComparisonType.Equal, 1337));

        Assert.True(NumberCompareHelper.Compare(42, NumericComparisonType.NotEqual, 1337));
        Assert.False(NumberCompareHelper.Compare(42, NumericComparisonType.NotEqual, 42));

        Assert.True(NumberCompareHelper.Compare(42, NumericComparisonType.LessThan, 1337));
        Assert.False(NumberCompareHelper.Compare(42, NumericComparisonType.LessThan, 42));

        Assert.True(NumberCompareHelper.Compare(42, NumericComparisonType.LessThanOrEqual, 1337));
        Assert.True(NumberCompareHelper.Compare(42, NumericComparisonType.LessThanOrEqual, 42));
        Assert.False(NumberCompareHelper.Compare(42, NumericComparisonType.LessThanOrEqual, 1));

        Assert.True(NumberCompareHelper.Compare(1337, NumericComparisonType.GreaterThan, 42));
        Assert.False(NumberCompareHelper.Compare(42, NumericComparisonType.GreaterThan, 1337));

        Assert.True(NumberCompareHelper.Compare(1337, NumericComparisonType.GreaterThanOrEqual, 42));
        Assert.True(NumberCompareHelper.Compare(1337, NumericComparisonType.GreaterThanOrEqual, 1337));
        Assert.False(NumberCompareHelper.Compare(42, NumericComparisonType.GreaterThanOrEqual, 1337));
    }
}
