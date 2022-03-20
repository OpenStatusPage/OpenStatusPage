using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Services;

public class ServiceStatusHistorySegmentDto
{
    public string ServiceId { get; set; }

    public DateTimeOffset From { get; set; }

    public DateTimeOffset? Until { get; set; }

    public List<Outage> Outages { get; set; }

    public class Outage
    {
        public DateTimeOffset From { get; set; }

        /// <summary>
        /// Timestamp that the outage lasted until. Null if still ongoing.
        /// </summary>
        public DateTimeOffset? Until { get; set; }

        public ServiceStatus ServiceStatus { get; set; }
    }
}
