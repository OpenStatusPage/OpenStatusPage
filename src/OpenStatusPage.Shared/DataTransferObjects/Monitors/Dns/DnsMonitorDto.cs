using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Dns
{
    public class DnsMonitorDto : MonitorDto
    {
        public string Hostname { get; set; }

        public string? Resolvers { get; set; }

        public DnsRecordType RecordType { get; set; }

        public List<DnsRecordRuleDto> DnsRecordRules { get; set; }
    }
}
