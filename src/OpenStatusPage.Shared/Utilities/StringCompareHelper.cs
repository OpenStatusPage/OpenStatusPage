using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.Utilities
{
    public class StringCompareHelper
    {
        public static bool Compare(string value, StringComparisonType comparisonType, string comparisonValue = default!)
        {
            return comparisonType switch
            {
                StringComparisonType.Equal => value.Equals(comparisonValue, StringComparison.OrdinalIgnoreCase),
                StringComparisonType.NotEqual => !value.Equals(comparisonValue, StringComparison.OrdinalIgnoreCase),
                StringComparisonType.Contains => value.Contains(comparisonValue, StringComparison.OrdinalIgnoreCase),
                StringComparisonType.NotContains => !value.Contains(comparisonValue, StringComparison.OrdinalIgnoreCase),
                StringComparisonType.Null => string.IsNullOrWhiteSpace(value),
                StringComparisonType.NotNull => !string.IsNullOrWhiteSpace(value),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
