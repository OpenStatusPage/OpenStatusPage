using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Ssh
{
    public class SshCommandResultRuleDto : MonitorRuleDto
    {
        public string ComparisonValue { get; set; }

        public StringComparisonType ComparisonType { get; set; }
    }
}
