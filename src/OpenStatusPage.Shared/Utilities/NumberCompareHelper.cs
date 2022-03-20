using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.Utilities
{
    public class NumberCompareHelper
    {
        public static bool Compare<T>(T value, NumericComparisonType comparisonType, T comparisonValue) where T : IComparable<T>
        {
            return comparisonType switch
            {
                NumericComparisonType.Equal => value.Equals(comparisonValue),
                NumericComparisonType.NotEqual => !value.Equals(comparisonValue),
                NumericComparisonType.LessThan => value.CompareTo(comparisonValue) < 0,
                NumericComparisonType.LessThanOrEqual => value.CompareTo(comparisonValue) <= 0,
                NumericComparisonType.GreaterThan => value.CompareTo(comparisonValue) > 0,
                NumericComparisonType.GreaterThanOrEqual => value.CompareTo(comparisonValue) >= 0,
                _ => throw new NotImplementedException(),
            };
        }
    }
}
