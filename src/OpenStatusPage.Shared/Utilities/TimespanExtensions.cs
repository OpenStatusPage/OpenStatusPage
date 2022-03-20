namespace OpenStatusPage.Shared.Utilities
{
    public static class TimespanExtensions
    {
        public static string DurationString(this TimeSpan duration)
        {
            var durationString = "";
            if (duration.Days > 0) durationString += $" {duration.Days} day{(duration.Days > 1 ? "s" : "")}";
            if (duration.Hours > 0) durationString += $" {duration.Hours} hour{(duration.Hours > 1 ? "s" : "")}";
            if (duration.Minutes > 0) durationString += $" {duration.Minutes} minute{(duration.Minutes > 1 ? "s" : "")}";
            if (duration.Seconds > 0) durationString += $" {duration.Seconds} second{(duration.Seconds > 1 ? "s" : "")}";

            return durationString;
        }
    }
}
