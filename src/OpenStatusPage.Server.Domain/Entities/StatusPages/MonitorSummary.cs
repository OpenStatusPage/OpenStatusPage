using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.StatusPages
{
    public class MonitorSummary : EntityBase
    {
        [Required]
        public string StatusPageId { get; set; }

        [Required]
        public int OrderIndex { get; set; }

        [Required]
        public string Title { get; set; }

        public bool ShowHistory { get; set; }

        public virtual ICollection<LabeledMonitor> LabeledMonitors { get; set; }

        public virtual StatusPage StatusPage { get; set; }
    }
}
