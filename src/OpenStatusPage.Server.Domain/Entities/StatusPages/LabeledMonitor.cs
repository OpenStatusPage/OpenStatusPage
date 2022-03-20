using OpenStatusPage.Server.Domain.Entities.Monitors;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.StatusPages
{
    public class LabeledMonitor : EntityBase
    {
        [Required]
        public string MonitorSummaryId { get; set; }

        [Required]
        public int OrderIndex { get; set; }

        [Required]
        public string MonitorId { get; set; }

        [Required]
        public string Label { get; set; }

        public virtual MonitorBase Monitor { get; set; }

        public virtual MonitorSummary MonitorSummary { get; set; }
    }
}
