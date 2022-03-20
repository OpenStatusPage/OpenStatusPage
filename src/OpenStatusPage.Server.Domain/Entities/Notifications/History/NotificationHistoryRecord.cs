using OpenStatusPage.Server.Domain.Entities.Monitors;
using System;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Notifications.History
{
    public class NotificationHistoryRecord
    {
        [Required]
        public string MonitorId { get; set; }

        [Required]
        public DateTime StatusUtc { get; set; } //Can not use DateTimeOffset due to SQLite support missing for comparison operations

        public virtual MonitorBase Monitor { get; set; }
    }
}
