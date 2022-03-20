namespace OpenStatusPage.Shared.DataTransferObjects.Monitors
{
    public class MonitorMetaDto : EntityBaseDto
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public bool Enabled { get; set; }

        public TimeSpan Interval { get; set; }
    }
}
