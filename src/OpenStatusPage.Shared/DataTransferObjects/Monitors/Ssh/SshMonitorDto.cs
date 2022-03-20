namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Ssh
{
    public class SshMonitorDto : MonitorDto
    {
        public string Hostname { get; set; }

        public ushort? Port { get; set; }

        public string Username { get; set; }

        public string? Password { get; set; }

        public string? PrivateKey { get; set; }

        public string? Command { get; set; }

        public List<SshCommandResultRuleDto> CommandResultRules { get; set; }
    }
}
