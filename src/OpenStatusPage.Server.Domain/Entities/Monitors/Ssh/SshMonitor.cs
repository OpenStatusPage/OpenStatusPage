using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors.Ssh
{
    public class SshMonitor : MonitorBase
    {
        [Required]
        public string Hostname { get; set; }

        public ushort? Port { get; set; }

        [Required]
        public string Username { get; set; }

        public string? Password { get; set; }

        public string? PrivateKey { get; set; }

        public string? Command { get; set; }
    }
}
