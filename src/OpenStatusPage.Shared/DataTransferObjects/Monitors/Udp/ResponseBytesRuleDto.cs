using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Udp
{
    public class ResponseBytesRuleDto : MonitorRuleDto
    {
        public string ExpectedBytes { get; set; }

        public BytesComparisonType ComparisonType { get; set; }
    }
}
