using Microsoft.AspNetCore.Components;
using MudBlazor;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;

namespace OpenStatusPage.Client.Pages.Dashboard.Monitors
{
    public partial class AddMonitorDialog
    {
        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        public string SelectedName { get; set; }

        public string SelectedValue { get; set; }

        private void AddProviderAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedName))
            {
                MudDialog.Close(DialogResult.Cancel());
            }

            MonitorMetaDto? type = SelectedValue switch
            {
                "HTTP" => new()
                {
                    Type = "HttpMonitor"
                },
                "PING" => new()
                {
                    Type = "PingMonitor"
                },
                "TCP" => new()
                {
                    Type = "TcpMonitor"
                },
                "UDP" => new()
                {
                    Type = "UdpMonitor"
                },
                "DNS" => new()
                {
                    Type = "DnsMonitor"
                },
                "SSH" => new()
                {
                    Type = "SshMonitor"
                },
                _ => null
            };

            if (type != null) type.Name = SelectedName;

            MudDialog.Close(DialogResult.Ok(type));
        }
    }
}