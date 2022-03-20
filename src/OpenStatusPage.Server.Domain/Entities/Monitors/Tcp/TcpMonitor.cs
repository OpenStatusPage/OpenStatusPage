using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors.Tcp;

public class TcpMonitor : MonitorBase
{
    [Required]
    public string Hostname { get; set; }

    [Required]
    public ushort Port { get; set; }
}
