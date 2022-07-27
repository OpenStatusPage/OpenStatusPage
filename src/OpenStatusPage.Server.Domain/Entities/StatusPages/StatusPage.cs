using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.StatusPages
{
    public class StatusPage : EntityBase
    {
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Optional display name that is different from the URL based route name
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Optional password if page is to be protected
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Optional description at the top of the page
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Enable a combined summary on top of the page to display operational, current incidents and maintenances
        /// </summary>
        [Required]
        public bool EnableGlobalSummary { get; set; }

        /// <summary>
        /// Enable a section under the global status to display upcoming maintenances
        /// </summary>
        [Required]
        public bool EnableUpcomingMaintenances { get; set; }

        /// <summary>
        /// Configure how far into the future maintenances are shown if enabled. <see cref="EnableUpcomingMaintenances"/>
        /// </summary>
        public int? DaysUpcomingMaintenances { get; set; }

        [Required]
        public int DaysStatusHistory { get; set; }

        [Required]
        public bool EnableIncidentTimeline { get; set; }

        /// <summary>
        /// How many days the incident timeline goes back if enabled. <see cref="EnableIncidentTimeline"/>
        /// </summary>
        public int? DaysIncidentTimeline { get; set; }

        public virtual ICollection<MonitorSummary> MonitorSummaries { get; set; }
    }
}
