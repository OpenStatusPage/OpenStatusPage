namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Tcp
{
    public class TcpMonitorDto : MonitorDto
    {
        public string Hostname { get; set; }

        public ushort Port { get; set; }

        public List<ResponseTimeRuleDto> ResponseTimeRules { get; set; }
    }
}
