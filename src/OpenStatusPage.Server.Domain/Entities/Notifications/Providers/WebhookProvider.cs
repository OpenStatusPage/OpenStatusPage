using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Notifications.Providers;

public class WebhookProvider : NotificationProvider
{
    [Required]
    public string Url { get; set; }

    public string? Headers { get; set; }
}
