using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Shared.Enumerations;
using System;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.StatusHistory
{
    public class StatusHistoryRecord
    {
        [Required]
        public string MonitorId { get; set; }

        [Required]
        public DateTime FromUtc { get; set; } //Can not use DateTimeOffset due to SQLite support missing for comparison operations

        [Required]
        public ServiceStatus Status { get; set; }

        public virtual MonitorBase Monitor { get; set; }
    }
}
