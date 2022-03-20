using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Utilities;
using Xunit;

namespace OpenStatusPage.Server.Tests.Facts.Utilities;

public class StringCompareHelperTests
{
    [Fact]
    public void TestAll()
    {
        // Assert
        Assert.True(StringCompareHelper.Compare("", StringComparisonType.Equal, ""));
        Assert.True(StringCompareHelper.Compare("a", StringComparisonType.Equal, "a"));
        Assert.False(StringCompareHelper.Compare("a", StringComparisonType.Equal, "b"));

        Assert.True(StringCompareHelper.Compare("a", StringComparisonType.NotEqual, "b"));
        Assert.False(StringCompareHelper.Compare("a", StringComparisonType.NotEqual, "a"));

        Assert.True(StringCompareHelper.Compare("a", StringComparisonType.Contains, "a"));
        Assert.True(StringCompareHelper.Compare("abc", StringComparisonType.Contains, "b"));
        Assert.False(StringCompareHelper.Compare("abc", StringComparisonType.Contains, "z"));

        Assert.True(StringCompareHelper.Compare("b", StringComparisonType.NotContains, "a"));
        Assert.True(StringCompareHelper.Compare("abc", StringComparisonType.NotContains, "z"));
        Assert.False(StringCompareHelper.Compare("abc", StringComparisonType.NotContains, "abc"));
        Assert.False(StringCompareHelper.Compare("b", StringComparisonType.NotContains, ""));

        Assert.True(StringCompareHelper.Compare(null!, StringComparisonType.Null));
        Assert.True(StringCompareHelper.Compare("", StringComparisonType.Null));

        Assert.True(StringCompareHelper.Compare("Lloyd", StringComparisonType.NotNull));
    }
}
