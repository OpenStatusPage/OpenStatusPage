using OpenStatusPage.Server.Domain.Entities.Monitors;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Incidents
{
    public class Incident : EntityBase
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public DateTimeOffset From { get; set; }

        /// <summary>
        /// If until is not set, the incident is ongoing.
        /// </summary>
        public DateTimeOffset? Until { get; set; }

        public virtual ICollection<MonitorBase> AffectedServices { get; set; }

        public virtual ICollection<IncidentTimelineItem> Timeline { get; set; }
    }
}
