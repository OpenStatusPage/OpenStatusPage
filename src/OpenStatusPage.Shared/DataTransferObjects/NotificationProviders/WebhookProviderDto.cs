namespace OpenStatusPage.Shared.DataTransferObjects.NotificationProviders
{
    public class WebhookProviderDto : NotificationProviderDto
    {
        public string Url { get; set; }

        public string Headers { get; set; }
    }
}
