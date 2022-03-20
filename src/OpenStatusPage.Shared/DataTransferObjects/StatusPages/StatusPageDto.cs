namespace OpenStatusPage.Shared.DataTransferObjects.StatusPages;

public class StatusPageDto : EntityBaseDto
{
    public string Name { get; set; }

    /// <summary>
    /// Optional description at the top of the page
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Enable a combined summary on top of the page to display operational, current incidents and maintenances
    /// </summary>
    public bool EnableGlobalSummary { get; set; }

    /// <summary>
    /// Enable a section under the global status to display upcoming maintenances
    /// </summary>
    public bool EnableUpcomingMaintenances { get; set; }

    /// <summary>
    /// Configure how far into the future maintenances are shown if enabled. <see cref="EnableUpcomingMaintenances"/>
    /// </summary>
    public int? DaysUpcomingMaintenances { get; set; }

    public int DaysStatusHistory { get; set; }

    public List<MonitorSummary> MonitorSummaries { get; set; }

    public bool EnableIncidentTimeline { get; set; }

    /// <summary>
    /// How many days the incident timeline goes back if enabled. <see cref="EnableIncidentTimeline"/>
    /// </summary>
    public int? DaysIncidentTimeline { get; set; }

    public class MonitorSummary : EntityBaseDto
    {
        public string StatusPageId { get; set; }

        public int OrderIndex { get; set; }

        public string Title { get; set; }

        public bool ShowHistory { get; set; }

        public List<LabeledMonitor> LabeledMonitors { get; set; }

        public class LabeledMonitor : EntityBaseDto
        {
            public string MonitorSummaryId { get; set; }

            public int OrderIndex { get; set; }

            public string MonitorId { get; set; }

            public string Label { get; set; }
        }
    }
}
