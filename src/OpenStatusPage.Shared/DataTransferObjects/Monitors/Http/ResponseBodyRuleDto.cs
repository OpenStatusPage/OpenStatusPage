using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Http
{
    public class ResponseBodyRuleDto : MonitorRuleDto
    {
        public string ComparisonValue { get; set; }

        public StringComparisonType ComparisonType { get; set; }
    }
}
