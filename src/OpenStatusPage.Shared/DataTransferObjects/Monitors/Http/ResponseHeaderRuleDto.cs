using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Http
{
    public class ResponseHeaderRuleDto : MonitorRuleDto
    {
        public string Key { get; set; }

        public string? ComparisonValue { get; set; }

        public StringComparisonType ComparisonType { get; set; }
    }
}
