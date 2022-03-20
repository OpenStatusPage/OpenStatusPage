using AutoMapper;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Client.Extensions;
using OpenStatusPage.Client.Pages.Dashboard.Monitors.Types;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Dns;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Http;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Ping;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Ssh;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Tcp;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Udp;
using OpenStatusPage.Shared.DataTransferObjects.NotificationProviders;
using OpenStatusPage.Shared.Requests;
using static OpenStatusPage.Client.Application.TransparentHttpClient;
using static OpenStatusPage.Client.Pages.Dashboard.Monitors.Types.DnsMonitor;
using static OpenStatusPage.Client.Pages.Dashboard.Monitors.Types.HttpMonitor;
using static OpenStatusPage.Client.Pages.Dashboard.Monitors.Types.MonitorBase;
using static OpenStatusPage.Client.Pages.Dashboard.Monitors.Types.PingMonitor;
using static OpenStatusPage.Client.Pages.Dashboard.Monitors.Types.SshMonitor;
using static OpenStatusPage.Client.Pages.Dashboard.Monitors.Types.TcpMonitor;
using static OpenStatusPage.Client.Pages.Dashboard.Monitors.Types.UdpMonitor;

namespace OpenStatusPage.Client.Pages.Dashboard.Monitors
{
    public partial class ManageMonitors : IAsyncDisposable
    {
        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public IDialogService DialogService { get; set; }

        [Inject]
        public TransparentHttpClient Http { get; set; }

        [CascadingParameter]
        protected HeaderEntry DashboardApiKeyHeader { get; set; }

        protected List<MonitorMetaDto> MonitorMetaData { get; set; }

        protected List<string> PossibleTags { get; set; }

        protected List<NotificationProviderMetaDto> NotificationProviderMetas { get; set; }

        protected MonitorMetaDto SelectedMonitor { get; set; }

        protected MonitorViewModel MonitorModel { get; set; }

        protected bool TryFetchData { get; set; } = true;

        protected string SearchTerm { get; set; }

        protected MudForm EditForm { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await RefreshMonitorsAsync();

            await base.OnInitializedAsync();
        }

        public async ValueTask DisposeAsync()
        {
            TryFetchData = false;
        }

        protected async Task RefreshMonitorsAsync()
        {
            while (TryFetchData)
            {
                //If we have no monitors yet, get them
                if (MonitorMetaData == null)
                {
                    var response = await Http.SendAsync<List<MonitorMetaDto>>(HttpMethod.Get, "api/v1/Monitors", DashboardApiKeyHeader);

                    if (response == null)
                    {
                        await Task.Delay(1000);

                        continue;
                    }

                    MonitorMetaData = response;
                }

                //Fetch the tags if we do not already have them
                if (PossibleTags == null)
                {
                    var response = await Http.SendAsync<List<string>>(HttpMethod.Get, "api/v1/ClusterMembers/tags", DashboardApiKeyHeader);

                    if (response == null)
                    {
                        await Task.Delay(1000);

                        continue;
                    }

                    PossibleTags = response;
                }

                //If we do have the list of status pages, get them
                if (NotificationProviderMetas == null)
                {
                    var response = await Http.SendAsync<List<NotificationProviderMetaDto>>(HttpMethod.Get, "api/v1/NotificationProviders", DashboardApiKeyHeader);

                    if (response == null)
                    {
                        await Task.Delay(1000);

                        continue;
                    }

                    NotificationProviderMetas = response;
                }

                break;
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnMonitorSelectedAsync(object newValue)
        {
            if (newValue is not MonitorMetaDto selectedMetaData || SelectedMonitor == selectedMetaData) return;

            await RefreshModelAsync(selectedMetaData);

            //If we can not build the provider model (server might be unavailable) deselect again.
            if (MonitorModel == null)
            {
                SelectedMonitor = null!;
                return;
            }

            selectedMetaData.Id = MonitorModel.Id;

            SelectedMonitor = selectedMetaData;
        }

        protected async Task RefreshModelAsync(MonitorMetaDto metaData)
        {
            var newModel = metaData.Type.ToLowerInvariant() switch
            {
                "dnsmonitor" => await DnsMonitor.LoadDataToModelAsync(Http, DashboardApiKeyHeader, Mapper, metaData),
                "httpmonitor" => await HttpMonitor.LoadDataToModelAsync(Http, DashboardApiKeyHeader, Mapper, metaData),
                "pingmonitor" => await PingMonitor.LoadDataToModelAsync(Http, DashboardApiKeyHeader, Mapper, metaData),
                "sshmonitor" => await SshMonitor.LoadDataToModelAsync(Http, DashboardApiKeyHeader, Mapper, metaData),
                "tcpmonitor" => await TcpMonitor.LoadDataToModelAsync(Http, DashboardApiKeyHeader, Mapper, metaData),
                "udpmonitor" => await UdpMonitor.LoadDataToModelAsync(Http, DashboardApiKeyHeader, Mapper, metaData),
                _ => null!,
            };

            if (newModel == null) return;

            MonitorModel = newModel;

            MonitorModel.Tags ??= new();
            MonitorModel.NotificationProviderMetas ??= NotificationProviderMetas.Where(x => x.DefaultForNewMonitors).ToList();

            metaData.Id = MonitorModel.Id;
            metaData.Name = MonitorModel.Name;
        }

        protected async Task SubmitChangeAsync()
        {
            await EditForm.Validate();

            if (!EditForm.IsValid) return;

            if (MonitorMetaData.Any(x =>
                !string.IsNullOrEmpty(x.Id) &&
                x.Id != MonitorModel.Id &&
                x.Name.ToLowerInvariant().Equals(MonitorModel.Name.ToLowerInvariant())))
            {
                Snackbar.Add("A monitor with the same name already exists.", Severity.Error);
                return;
            }

            MonitorDto sendContainer = MonitorModel switch
            {
                DnsMonitorViewModel => new DnsMonitorDto(),
                HttpMonitorViewModel => new HttpMonitorDto(),
                PingMonitorViewModel => new PingMonitorDto(),
                SshMonitorViewModel => new SshMonitorDto(),
                TcpMonitorViewModel => new TcpMonitorDto(),
                UdpMonitorViewModel => new UdpMonitorDto(),
                _ => null!,
            };

            if (sendContainer == null) return;

            //Apply changes from view model onto selected instance
            Mapper.Map(MonitorModel, sendContainer);

            //Submit with proposed id if it does't already have one, so the request replication is deterministic
            sendContainer.Id ??= Guid.NewGuid().ToString();

            //Increase version
            sendContainer.Version++;

            switch (sendContainer)
            {
                case DnsMonitorDto dnsMonitor:
                {
                    dnsMonitor.DnsRecordRules.ForEach(x => { x.Id ??= Guid.NewGuid().ToString(); x.MonitorId = sendContainer.Id; x.Version++; });
                    break;
                }

                case HttpMonitorDto httpMonitor:
                {
                    httpMonitor.ResponseBodyRules.ForEach(x => { x.Id ??= Guid.NewGuid().ToString(); x.MonitorId = sendContainer.Id; x.Version++; });
                    httpMonitor.ResponseHeaderRules.ForEach(x => { x.Id ??= Guid.NewGuid().ToString(); x.MonitorId = sendContainer.Id; x.Version++; });
                    httpMonitor.ResponseTimeRules.ForEach(x => { x.Id ??= Guid.NewGuid().ToString(); x.MonitorId = sendContainer.Id; x.Version++; });
                    httpMonitor.SslCertificateRules.ForEach(x => { x.Id ??= Guid.NewGuid().ToString(); x.MonitorId = sendContainer.Id; x.Version++; });
                    httpMonitor.StatusCodeRules.ForEach(x => { x.Id ??= Guid.NewGuid().ToString(); x.MonitorId = sendContainer.Id; x.Version++; });
                    break;
                }

                case PingMonitorDto pingMonitor:
                {
                    pingMonitor.ResponseTimeRules.ForEach(x => { x.Id ??= Guid.NewGuid().ToString(); x.MonitorId = sendContainer.Id; x.Version++; });
                    break;
                }

                case SshMonitorDto sshMonitor:
                {
                    sshMonitor.CommandResultRules.ForEach(x => { x.Id ??= Guid.NewGuid().ToString(); x.MonitorId = sendContainer.Id; x.Version++; });
                    break;
                }

                case TcpMonitorDto tcpMonitor:
                {
                    tcpMonitor.ResponseTimeRules.ForEach(x => { x.Id ??= Guid.NewGuid().ToString(); x.MonitorId = sendContainer.Id; x.Version++; });
                    break;
                }

                case UdpMonitorDto udpMonitor:
                {
                    udpMonitor.ResponseTimeRules.ForEach(x => { x.Id ??= Guid.NewGuid().ToString(); x.MonitorId = sendContainer.Id; x.Version++; });
                    udpMonitor.ResponseBytesRules.ForEach(x => { x.Id ??= Guid.NewGuid().ToString(); x.MonitorId = sendContainer.Id; x.Version++; });
                    break;
                }

                default: throw new Exception("Unknown dto type.");
            }

            var response = await Http.SendAsync<SuccessResponse>(
                HttpMethod.Post,
                $"api/v1/Monitors?typename={sendContainer.GetType().Name}",
                sendContainer,
                DashboardApiKeyHeader);

            if (response != null && response.WasSuccessful)
            {
                //Create/Update was successful, so update the meta data id for the model refresh call.
                SelectedMonitor.Id = sendContainer.Id;

                //Rebuild view model with new data
                await RefreshModelAsync(SelectedMonitor);

                Snackbar.Add("Changes saved successfully", Severity.Success);
            }
            else
            {
                //Create/Update failed, so refresh the server data if the entity already existed
                if (!string.IsNullOrEmpty(SelectedMonitor.Id))
                {
                    //Rebuild view model with new data
                    await RefreshModelAsync(SelectedMonitor);
                }

                Snackbar.Add("Changes could not be saved", Severity.Error);
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task RemoveMonitorAsync()
        {
            if (await DialogService.ConfirmAsync(
                $"Delete monitor {SelectedMonitor.Name}",
                $"Are you sure you want to delete the monitor {SelectedMonitor.Name}?",
                "Delete",
                submitColor: Color.Error,
                confirmIcon: Icons.Outlined.DeleteForever))
            {

                //No id means it was not persistent yet
                bool success = string.IsNullOrEmpty(SelectedMonitor.Id);

                if (!success)
                {
                    var response = await Http.SendAsync<SuccessResponse>(HttpMethod.Delete, $"api/v1/Monitors/{SelectedMonitor.Id}", DashboardApiKeyHeader);

                    success = response != null && response.WasSuccessful;
                }

                if (success)
                {
                    //Remove instance
                    MonitorMetaData.Remove(SelectedMonitor);

                    //Deselect provider
                    SelectedMonitor = null!;

                    await InvokeAsync(StateHasChanged);

                    Snackbar.Add("Monitor deleted successfully", Severity.Success);
                }
                else
                {
                    Snackbar.Add("There was a problem deleting the monitor", Severity.Error);
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task OpenAddMonitorAsync()
        {
            var result = await DialogService.Show<AddMonitorDialog>("Add a new monitor", new DialogOptions()
            {
                FullWidth = true,
                CloseOnEscapeKey = true,
                CloseButton = true,
                MaxWidth = MaxWidth.Small
            }).Result;

            if (result.Cancelled || result.Data is not MonitorMetaDto newMonitor) return;

            if (MonitorMetaData.Any(x => x.Name.ToLowerInvariant().Equals(newMonitor.Name.ToLowerInvariant())))
            {
                Snackbar.Add("A monitor with the same name already exists.", Severity.Error);
                return;
            }

            MonitorMetaData.Add(newMonitor);

            await OnMonitorSelectedAsync(newMonitor);
        }

        protected static string MetaToTypeString(MonitorMetaDto meta)
        {
            return meta.Type.ToLowerInvariant() switch
            {
                "dnsmonitor" => "DNS",
                "httpmonitor" => "HTTP",
                "pingmonitor" => "PING",
                "sshmonitor" => "SSH",
                "tcpmonitor" => "TCP",
                "udpmonitor" => "UDP",
                _ => "Unknown"
            };
        }

        protected void AddNotificationProvider(NotificationProviderMetaDto newProvider)
        {
            if (newProvider != null) MonitorModel.NotificationProviderMetas.Add(newProvider);
        }

        protected void RemoveNotificationProvider(NotificationProviderMetaDto providerDto)
        {
            MonitorModel.NotificationProviderMetas.Remove(providerDto);
        }

        protected async Task<IEnumerable<NotificationProviderMetaDto>> SearchProviderAsync(string value)
        {
            var possible = NotificationProviderMetas.Where(x => !MonitorModel.NotificationProviderMetas.Any(y => y.Id == x.Id)).ToList();

            // if text is null or empty, show complete list
            if (string.IsNullOrEmpty(value)) return possible;

            return possible.Where(x =>
                x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                x.Type.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        protected void AddTag(string tag)
        {
            MonitorModel.Tags.Add(tag);
        }

        protected void RemoveTag(string tag)
        {
            MonitorModel.Tags.Remove(tag);
        }

        protected async Task<IEnumerable<string>> SearchTagAsync(string value)
        {
            var possible = PossibleTags.Where(x => !MonitorModel.Tags.Contains(x)).ToList();

            // if text is null or empty, show complete list
            if (string.IsNullOrEmpty(value)) return possible;

            return possible.Where(x => x.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
