using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors.Udp
{
    public class UdpMonitor : MonitorBase
    {
        [Required]
        public string Hostname { get; set; }

        [Required]
        public ushort Port { get; set; }

        [Required]
        public byte[] RequestBytes { get; set; }
    }
}
