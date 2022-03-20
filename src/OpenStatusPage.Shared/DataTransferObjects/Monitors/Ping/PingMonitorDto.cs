namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Ping
{
    public class PingMonitorDto : MonitorDto
    {
        public string Hostname { get; set; }

        public List<ResponseTimeRuleDto> ResponseTimeRules { get; set; }
    }
}
