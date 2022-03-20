using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Shared.Models.Webhooks
{
    public class StatusWebhook
    {
        [Required]
        public string MonitorId { get; set; }

        [Required]
        public string MonitorDisplayName { get; set; }

        public ServiceStatus? OldStatus { get; set; }

        public long? OldUnixSecondsUtc { get; set; }

        [Required]
        public ServiceStatus NewStatus { get; set; }

        [Required]
        public long NewUnixSecondsUtc { get; set; }
    }
}
