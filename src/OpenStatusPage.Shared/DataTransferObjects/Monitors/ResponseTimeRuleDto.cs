using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Monitors
{
    public class ResponseTimeRuleDto : MonitorRuleDto
    {
        public ushort ComparisonValue { get; set; }

        public NumericComparisonType ComparisonType { get; set; }
    }
}
