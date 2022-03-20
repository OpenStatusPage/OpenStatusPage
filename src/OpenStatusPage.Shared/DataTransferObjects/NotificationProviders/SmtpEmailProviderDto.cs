namespace OpenStatusPage.Shared.DataTransferObjects.NotificationProviders
{
    public class SmtpEmailProviderDto : NotificationProviderDto
    {
        public string Hostname { get; set; }

        public ushort? Port { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string? DisplayName { get; set; }

        public string? FromAddress { get; set; }

        public string? ReceiversDirect { get; set; }

        public string? ReceiversCC { get; set; }

        public string? ReceiversBCC { get; set; }
    }
}
