namespace OpenStatusPage.Shared.Utilities
{
    public static class DateTimeOffsetExtensions
    {
        public static bool IsInRangeInclusive(this DateTimeOffset dateTime, DateTimeOffset from, DateTimeOffset until)
        {
            return (from <= dateTime) && (dateTime <= until);
        }

        public static bool IsInRangeExclusive(this DateTimeOffset dateTime, DateTimeOffset from, DateTimeOffset until)
        {
            return (from <= dateTime) && (dateTime < until);
        }

        public static bool IsInRangeInclusiveNullable(this DateTimeOffset dateTime, DateTimeOffset from, DateTimeOffset? until)
        {
            return (from <= dateTime) && (!until.HasValue || (dateTime <= until));
        }

        public static bool IsInRangeExclusiveNullable(this DateTimeOffset dateTime, DateTimeOffset from, DateTimeOffset? until)
        {
            return (from <= dateTime) && (!until.HasValue || (dateTime < until));
        }
    }
}
