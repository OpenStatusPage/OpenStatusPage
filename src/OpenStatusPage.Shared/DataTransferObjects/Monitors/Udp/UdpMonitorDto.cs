namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Udp
{
    public class UdpMonitorDto : MonitorDto
    {
        public string Hostname { get; set; }

        public ushort Port { get; set; }

        public string RequestBytes { get; set; }

        public List<ResponseTimeRuleDto> ResponseTimeRules { get; set; }

        public List<ResponseBytesRuleDto> ResponseBytesRules { get; set; }
    }
}
