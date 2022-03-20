namespace OpenStatusPage.Shared.DataTransferObjects.NotificationProviders
{
    public class NotificationProviderDto : EntityBaseDto
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }

        public bool DefaultForNewMonitors { get; set; }
    }
}
