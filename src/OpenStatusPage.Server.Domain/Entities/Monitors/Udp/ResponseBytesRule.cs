using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors.Udp
{
    public class ResponseBytesRule : MonitorRule
    {
        [Required]
        public byte[] ExpectedBytes { get; set; }

        [Required]
        public BytesComparisonType ComparisonType { get; set; }
    }
}
