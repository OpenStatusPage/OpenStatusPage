using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors.Ping
{
    public class PingMonitor : MonitorBase
    {
        [Required]
        public string Hostname { get; set; }
    }
}
