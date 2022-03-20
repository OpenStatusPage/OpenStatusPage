using MailKit.Net.Smtp;
using MimeKit;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Domain.Entities.StatusHistory;
using OpenStatusPage.Shared.Utilities;
using System.Globalization;

namespace OpenStatusPage.Server.Application.Notifications.Senders
{
    public class SmtpEmailSender : INotificationSender
    {
        private readonly SmtpEmailProvider _smtpEmailProvider;

        public SmtpEmailSender(SmtpEmailProvider smtpEmailProvider)
        {
            _smtpEmailProvider = smtpEmailProvider;
        }

        public async Task SendNotificationAsync(MonitorBase monitor, StatusHistoryRecord? previous, StatusHistoryRecord current)
        {
            var message = new MimeMessage();

            (_smtpEmailProvider.ReceiversDirect ?? "")
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList()
                .ForEach(direct => message.To.Add(new MailboxAddress(direct, direct)));

            (_smtpEmailProvider.ReceiversCC ?? "")
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList()
                .ForEach(cc => message.Cc.Add(new MailboxAddress(cc, cc)));

            (_smtpEmailProvider.ReceiversBCC ?? "")
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList()
                .ForEach(bcc => message.Bcc.Add(new MailboxAddress(bcc, bcc)));

            //No receivers, skip the notification sending process
            if (!(message.To.Any() || message.Cc.Any() || message.Bcc.Any())) return;

            message.From.Add(new MailboxAddress(_smtpEmailProvider.DisplayName ?? "OpenStatusPage", _smtpEmailProvider.FromAddress ?? _smtpEmailProvider.Username));

            message.Subject = $"Status for '{monitor.Name}' has changed to '{Enum.GetName(current.Status)}' since {current.FromUtc.ToString("G", CultureInfo.GetCultureInfo("de-DE"))} (UTC)";

            var body = $"{monitor.Name} ({monitor.Id}) is '{Enum.GetName(current.Status)}' since {current.FromUtc.ToString("G", CultureInfo.GetCultureInfo("de-DE"))} (UTC).\n";

            if (previous != null)
            {
                var durationString = (current.FromUtc - previous.FromUtc).DurationString();

                if (!string.IsNullOrWhiteSpace(durationString))
                {
                    body += $"Previous status was '{Enum.GetName(previous.Status)}' for{durationString}.\n";
                }
            }

            body += $"\nThis message was sent automatically via OpenStatusPage.";

            message.Body = new MultipartAlternative { new TextPart(MimeKit.Text.TextFormat.Plain) { Text = body } };

            using var smptClient = new SmtpClient();

            await smptClient.ConnectAsync(_smtpEmailProvider.Hostname, _smtpEmailProvider.Port ?? 0, MailKit.Security.SecureSocketOptions.Auto);

            await smptClient.AuthenticateAsync(_smtpEmailProvider.Username, _smtpEmailProvider.Password);

            await smptClient.SendAsync(message);

            await smptClient.DisconnectAsync(true);
        }
    }
}
