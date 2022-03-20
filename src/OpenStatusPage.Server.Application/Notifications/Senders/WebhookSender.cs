using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Domain.Entities.StatusHistory;
using OpenStatusPage.Shared.Models.Webhooks;
using System.Net.Http.Json;

namespace OpenStatusPage.Server.Application.Notifications.Senders
{
    public class WebhookSender : INotificationSender
    {
        private readonly WebhookProvider _webhookProvider;

        public WebhookSender(WebhookProvider webhookProvider)
        {
            _webhookProvider = webhookProvider;
        }

        public async Task SendNotificationAsync(MonitorBase monitor, StatusHistoryRecord? previous, StatusHistoryRecord current)
        {
            var data = new StatusWebhook()
            {
                MonitorId = monitor.Id,
                MonitorDisplayName = monitor.Name,
                NewStatus = current.Status,
                NewUnixSecondsUtc = ((DateTimeOffset)current.FromUtc).ToUnixTimeSeconds()
            };

            if (previous != null)
            {
                data.OldStatus = previous.Status;
                data.OldUnixSecondsUtc = ((DateTimeOffset)previous.FromUtc).ToUnixTimeSeconds();
            }

            using var httpClient = new HttpClient();

            //Set request header
            var headers = _webhookProvider.Headers?
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(pair => pair.Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) //Split only on first =
                .GroupBy(x => x[0]) //Group by header key
                .ToList() ?? new();

            foreach (var header in headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.SelectMany(x => x));
            }

            var respose = await httpClient.PostAsJsonAsync(_webhookProvider.Url, data);

            respose.EnsureSuccessStatusCode();
        }
    }
}
