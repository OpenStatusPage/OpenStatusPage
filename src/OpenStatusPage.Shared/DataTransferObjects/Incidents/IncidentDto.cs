using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Incidents;

public class IncidentDto : EntityBaseDto
{
    public string Name { get; set; }

    public DateTimeOffset From { get; set; }

    /// <summary>
    /// If until is not set, the incident is ongoing.
    /// </summary>
    public DateTimeOffset? Until { get; set; }

    /// <summary>
    /// List of service ids that were associated with this incident at some time.
    /// </summary>
    public List<string> AffectedServices { get; set; }

    /// <summary>
    /// Timeline to record the incident progress
    /// </summary>
    public List<IncidentTimelineItem> Timeline { get; set; }

    public class IncidentTimelineItem : EntityBaseDto
    {
        public DateTimeOffset DateTime { get; set; }

        public IncidentSeverity Severity { get; set; }

        public IncidentStatus Status { get; set; }

        /// <summary>
        /// Optional information given about the severity change.
        /// </summary>
        public string? AdditionalInformation { get; set; }
    }
}
