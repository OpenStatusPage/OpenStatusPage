using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Notifications.Providers;

public class SmtpEmailProvider : NotificationProvider
{
    [Required]
    public string Hostname { get; set; }

    public ushort? Port { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public string Password { get; set; }

    public string? DisplayName { get; set; }

    public string? FromAddress { get; set; }

    public string? ReceiversDirect { get; set; }

    public string? ReceiversCC { get; set; }

    public string? ReceiversBCC { get; set; }
}
