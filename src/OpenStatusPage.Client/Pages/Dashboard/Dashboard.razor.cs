using Microsoft.AspNetCore.Components;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Shared.DataTransferObjects.Incidents;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.StatusPages;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Requests.Services;
using OpenStatusPage.Shared.Utilities;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard
{
    public partial class Dashboard : IAsyncDisposable
    {
        [Inject]
        public TransparentHttpClient Http { get; set; }

        [CascadingParameter]
        protected HeaderEntry DashboardApiKeyHeader { get; set; }

        protected List<IncidentMetaDto> IncidentMetas { get; set; }

        protected List<MonitorMetaDto> MonitorMetas { get; set; }

        protected List<StatusPageMetaDto> StatusPageMetas { get; set; }

        protected DateTimeOffset? LastRefresh { get; set; }

        protected int? RefreshSecondsRemaining { get; set; }

        protected Timer RefreshTimer { get; set; }

        protected bool RefreshInProgress { get; set; }

        protected bool TryFetchData { get; set; } = true;

        protected int ActiveIncidentsCount { get; set; }

        protected int PublicStatusPagesCount { get; set; }

        protected int AvailableMonitorsCount { get; set; }

        protected int DegradedMonitorsCount { get; set; }

        protected int UnavailableMonitorsCount { get; set; }

        protected List<MonitorDataStruct> MonitorData { get; set; }

        protected string _searchString;

        protected override async Task OnInitializedAsync()
        {
            await RefreshDataAsync();

            await base.OnInitializedAsync();
        }

        public async ValueTask DisposeAsync()
        {
            TryFetchData = false;

            if (RefreshTimer != null) await RefreshTimer.DisposeAsync();
        }

        protected async Task RefreshDataAsync()
        {
            while (TryFetchData)
            {
                //If any refresh timer is running stop it
                RefreshTimer?.Change(Timeout.Infinite, 0);

                var monitors = await Http.SendAsync<List<MonitorMetaDto>>(HttpMethod.Get, "api/v1/Monitors", DashboardApiKeyHeader);

                if (monitors == null)
                {
                    await Task.Delay(1000);

                    continue;
                }

                //We only care about the enabled ones
                monitors = monitors
                    .Where(x => x.Enabled)
                    .ToList();

                var outagesRequest = new ServiceStatusHistoryRequest()
                {
                    ServiceIds = monitors.Select(x => x.Id).Distinct().ToList(),
                    From = DateTimeOffset.UtcNow,
                    Until = DateTimeOffset.Now.Date.ToUniversalTime().AddDays(1) //Request includes everyhting until the end of the day
                };

                var serviceHistories = (await Http.SendAsync<ServiceStatusHistoryRequest, ServiceStatusHistoryRequest.Response>(HttpMethod.Post, $"api/v1/ServiceStatusHistories/public/bulk", outagesRequest))?.ServiceStatusHistories;

                var incidents = await Http.SendAsync<List<IncidentMetaDto>>(HttpMethod.Get, "api/v1/Incidents", DashboardApiKeyHeader);

                var statusPages = await Http.SendAsync<List<StatusPageMetaDto>>(HttpMethod.Get, "api/v1/StatusPages", DashboardApiKeyHeader);

                if (incidents == null || statusPages == null || serviceHistories == null)
                {
                    await Task.Delay(1000);

                    continue;
                }

                //Incident processing
                IncidentMetas = incidents;

                ActiveIncidentsCount = IncidentMetas.Count(x => x.LatestStatus != IncidentStatus.Resolved);

                //Monitors processing
                MonitorMetas = monitors;

                MonitorData = new();

                foreach (var monitor in monitors)
                {
                    var histories = serviceHistories
                        .Where(x => x.ServiceId == monitor.Id && DateTimeOffset.UtcNow.IsInRangeInclusiveNullable(x.From, x.Until))
                        .ToList();

                    var status = ServiceStatus.Unknown;

                    if (histories.Count > 0)
                    {
                        status = histories
                            .SelectMany(x => x.Outages)
                            .MaxBy(x => x.ServiceStatus)?.ServiceStatus ?? ServiceStatus.Available;
                    }

                    var type = monitor.Type.ToLowerInvariant() switch
                    {
                        "dnsmonitor" => "DNS",
                        "httpmonitor" => "HTTP",
                        "pingmonitor" => "PING",
                        "sshmonitor" => "SSH",
                        "tcpmonitor" => "TPC",
                        "udpmonitor" => "UDP",
                        _ => "Unknown",
                    };

                    //Try and guess the next execution based on when the history records were written
                    var nextExecution = histories
                        .Where(x => x.From.Millisecond == 0) //Records written from sync intervals and not regular flushes are likely the only ones with ms == 0
                        .MinBy(x => x.From)?.From ?? DateTimeOffset.UtcNow.UtcDateTime.Date;
                    while (nextExecution < DateTimeOffset.UtcNow) nextExecution += monitor.Interval;

                    MonitorData.Add(new(type, monitor.Name, status, monitor.Interval, nextExecution));
                }

                AvailableMonitorsCount = MonitorData.Count(x => x.Status == ServiceStatus.Available);
                DegradedMonitorsCount = MonitorData.Count(x => x.Status == ServiceStatus.Degraded);
                UnavailableMonitorsCount = MonitorData.Count(x => x.Status == ServiceStatus.Unavailable);

                //Status pages processing
                StatusPageMetas = statusPages;

                PublicStatusPagesCount = statusPages.Count(x => x.IsPublic);

                //Start refresh interval
                LastRefresh = DateTimeOffset.UtcNow;

                RefreshSecondsRemaining = 60;

                RefreshTimer = new(RefreshCountDownTickAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

                RefreshInProgress = false;

                break;
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async void RefreshCountDownTickAsync(object? state)
        {
            RefreshSecondsRemaining--;

            if (RefreshSecondsRemaining > 0)
            {
                await InvokeAsync(StateHasChanged);
            }
            else
            {
                await TriggerRefreshAsync();
            }
        }

        protected async Task TriggerRefreshAsync()
        {
            RefreshInProgress = true;

            //Ui update to show refresh in progress
            await InvokeAsync(StateHasChanged);

            //Force an update
            await RefreshDataAsync();
        }

        protected bool FilterFunc(MonitorDataStruct monitor)
        {
            if (string.IsNullOrWhiteSpace(_searchString)) return true;

            if (monitor.Type.Contains(_searchString, StringComparison.OrdinalIgnoreCase)) return true;

            if (monitor.Name.Contains(_searchString, StringComparison.OrdinalIgnoreCase)) return true;

            if (Enum.GetName(monitor.Status).Contains(_searchString, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        public class MonitorDataStruct
        {
            public MonitorDataStruct(string type, string name, ServiceStatus status, TimeSpan interval, DateTimeOffset nextExecution)
            {
                Type = type;
                Name = name;
                Status = status;
                Interval = interval;
                NextExecution = nextExecution;
            }

            public string Type { get; }

            public string Name { get; }

            public ServiceStatus Status { get; }

            public TimeSpan Interval { get; }

            public DateTimeOffset NextExecution { get; set; }
        }
    }
}