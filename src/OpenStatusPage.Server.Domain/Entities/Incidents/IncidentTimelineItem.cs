using OpenStatusPage.Shared.Enumerations;
using System;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Incidents
{
    public class IncidentTimelineItem : EntityBase
    {
        [Required]
        public string IncidentId { get; set; }

        [Required]
        public DateTimeOffset DateTime { get; set; }

        [Required]
        public IncidentSeverity Severity { get; set; }

        [Required]
        public IncidentStatus Status { get; set; }

        /// <summary>
        /// Optional information given about the severity change.
        /// </summary>
        public string? AdditionalInformation { get; set; }

        public virtual Incident Incident { get; set; }
    }
}
