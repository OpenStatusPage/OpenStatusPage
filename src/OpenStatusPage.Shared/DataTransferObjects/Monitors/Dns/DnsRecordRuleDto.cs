using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Dns
{
    public class DnsRecordRuleDto : MonitorRuleDto
    {
        public string ComparisonValue { get; set; }

        public StringComparisonType ComparisonType { get; set; }
    }
}
