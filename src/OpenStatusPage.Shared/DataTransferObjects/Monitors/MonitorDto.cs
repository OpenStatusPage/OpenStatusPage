using OpenStatusPage.Shared.DataTransferObjects.NotificationProviders;

namespace OpenStatusPage.Shared.DataTransferObjects.Monitors
{
    public class MonitorDto : EntityBaseDto
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }

        public TimeSpan Interval { get; set; }

        public ushort? Retries { get; set; }

        public TimeSpan? RetryInterval { get; set; }

        public TimeSpan? Timeout { get; set; }

        public int WorkerCount { get; set; }

        public string Tags { get; set; }

        public List<NotificationProviderMetaDto> NotificationProviderMetas { get; set; }
    }
}
