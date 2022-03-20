using OpenStatusPage.Server.Domain.Entities.Monitors;

namespace OpenStatusPage.Server.Application.Monitors
{
    public static class MonitorExtensions
    {
        public static List<string> GetTags(this MonitorBase monitor)
        {
            return monitor.Tags.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}
