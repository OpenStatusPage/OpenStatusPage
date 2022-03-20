namespace OpenStatusPage.Shared.DataTransferObjects.NotificationProviders
{
    public class NotificationProviderMetaDto : EntityBaseDto
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public bool DefaultForNewMonitors { get; set; }
    }
}
