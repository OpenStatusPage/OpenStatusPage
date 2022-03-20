using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Monitors
{
    public class MonitorRuleDto : EntityBaseDto
    {
        public string MonitorId { get; set; }

        public ushort OrderIndex { get; set; }

        public ServiceStatus ViolationStatus { get; set; }
    }
}
