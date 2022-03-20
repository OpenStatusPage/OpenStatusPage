using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors.Dns
{
    public class DnsMonitor : MonitorBase
    {
        [Required]
        public string Hostname { get; set; }

        /// <summary>
        /// Comma seperated list of resolver servers
        /// </summary>
        public string? Resolvers { get; set; }

        [Required]
        public DnsRecordType RecordType { get; set; }
    }
}
