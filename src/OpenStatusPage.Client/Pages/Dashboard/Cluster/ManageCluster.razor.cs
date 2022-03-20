using Microsoft.AspNetCore.Components;
using MudBlazor;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Client.Extensions;
using OpenStatusPage.Shared.DataTransferObjects.Cluster;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Requests;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard.Cluster
{
    public partial class ManageCluster : IAsyncDisposable
    {
        [Inject]
        public TransparentHttpClient Http { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public IDialogService DialogService { get; set; }

        [CascadingParameter]
        protected HeaderEntry DashboardApiKeyHeader { get; set; }

        protected List<ClusterMemberDto> ClusterMembers { get; set; }

        protected DateTimeOffset? LastRefresh { get; set; }

        protected int? RefreshSecondsRemaining { get; set; }

        protected Timer RefreshTimer { get; set; }

        protected bool RefreshInProgress { get; set; }

        protected bool TryFetchData { get; set; } = true;

        public string _searchString;

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

                var response = await Http.SendAsync<List<ClusterMemberDto>>(HttpMethod.Get, "api/v1/ClusterMembers", DashboardApiKeyHeader);

                if (response == null)
                {
                    await Task.Delay(1000);

                    continue;
                }

                ClusterMembers = response;

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

        protected bool FilterFunc(ClusterMemberDto member)
        {
            if (string.IsNullOrWhiteSpace(_searchString)) return true;

            if (member.Endpoint.ToString().Contains(_searchString, StringComparison.OrdinalIgnoreCase)) return true;

            if (member.Tags.Any(x => x.Contains(_searchString, StringComparison.OrdinalIgnoreCase))) return true;

            return false;
        }

        protected async Task RemoveClusterMemberAsync(ClusterMemberDto member)
        {
            if (await DialogService.ConfirmAsync(
                $"Remove cluster member {member.Endpoint}",
                $"Are you sure you want to shut down and remove the member {member.Endpoint} from the cluster? All jobs will be taken over by other members if possible, but any unproccssed data might get lost.",
                "Remove",
                submitColor: Color.Error,
                confirmIcon: Icons.Outlined.PowerOff))
            {
                var response = await Http.SendAsync<SuccessResponse>(HttpMethod.Delete, $"api/v1/ClusterMembers/{member.Id}", member.Endpoint, DashboardApiKeyHeader);

                if (response != null && response.WasSuccessful)
                {
                    Snackbar.Add("Member was successfully removed from the cluster.", Severity.Success);
                }
                else
                {
                    Snackbar.Add("There was a problem removing the member from the cluster.", Severity.Error);
                }

                await RefreshDataAsync();
            }
        }

        protected int CurrentlyAvailable()
        {
            return ClusterMembers.Where(x => x.Availability == ClusterMemberAvailability.Available).Count();
        }

        protected int CountUntilDataLoss()
        {
            var possibleFailues = (ClusterMembers.Count - 1) / 2;

            return possibleFailues - ClusterMembers.Where(x => x.Availability != ClusterMemberAvailability.Available).Count();
        }
    }
}