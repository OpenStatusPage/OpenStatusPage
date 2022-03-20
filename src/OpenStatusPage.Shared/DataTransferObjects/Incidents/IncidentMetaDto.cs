using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Incidents
{
    public class IncidentMetaDto : EntityBaseDto
    {
        public string Name { get; set; }

        public IncidentStatus LatestStatus { get; set; }

        public IncidentSeverity LatestSeverity { get; set; }

        public string LastestTimelineItemId { get; set; }
    }
}
