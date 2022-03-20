namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Http
{
    public class StatusCodeRuleDto : MonitorRuleDto
    {
        public ushort Value { get; set; }

        public ushort? UpperRangeValue { get; set; }
    }
}
