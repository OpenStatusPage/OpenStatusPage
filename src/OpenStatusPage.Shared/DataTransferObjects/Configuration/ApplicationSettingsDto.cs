namespace OpenStatusPage.Shared.DataTransferObjects.Configuration
{
    public class ApplicationSettingsDto : EntityBaseDto
    {
        public TimeSpan StatusFlushInterval { get; set; }

        public ushort DaysMonitorHistory { get; set; }

        public ushort DaysIncidentHistory { get; set; }

        public string DefaultStatusPageId { get; set; }
    }
}
