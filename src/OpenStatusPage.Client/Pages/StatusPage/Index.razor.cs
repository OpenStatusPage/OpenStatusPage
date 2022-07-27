using Ganss.XSS;
using Markdig;
using Microsoft.AspNetCore.Components;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Client.Application.Authentication;
using OpenStatusPage.Shared.DataTransferObjects.Incidents;
using OpenStatusPage.Shared.DataTransferObjects.Services;
using OpenStatusPage.Shared.DataTransferObjects.StatusPages;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Models.Credentials;
using OpenStatusPage.Shared.Requests.Incidents;
using OpenStatusPage.Shared.Requests.Services;
using OpenStatusPage.Shared.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.StatusPage
{
    public partial class Index : IAsyncDisposable
    {
        [Parameter]
        public string StatusPageId { get; set; }

        [Inject]
        public TransparentHttpClient Http { get; set; }

        [Inject]
        public CredentialService CredentialService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        protected StatusPageDto StatusPageConfiguration { get; set; }

        protected List<IncidentDto> Incidents { get; set; }

        protected List<ServiceStatusHistorySegmentDto> ServiceStatusHistories { get; set; }

        public HeaderEntry AccessToken { get; set; }

        protected bool Unauthorized { get; set; }

        protected LoginModel LoginViewModel { get; set; }

        protected DateTimeOffset? LastRefresh { get; set; }

        protected int? RefreshSecondsRemaining { get; set; }

        protected Timer RefreshTimer { get; set; }

        protected bool RefreshInProgress { get; set; }

        protected bool TryFetchData { get; set; } = true;

        //Cache processed data
        public ServiceStatus? CurrentWorstServiceStatus { get; set; }

        public IncidentSeverity? CurrentMaxIncidentSeverity { get; set; }

        protected List<IncidentDto> OngoingIncidents { get; set; }

        protected List<IncidentDto> UpcomingMaintenances { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (string.IsNullOrWhiteSpace(StatusPageId))
            {
                StatusPageId = "default";
            }

            await ReloadStatusPageConfigurationAsync();

            await base.OnParametersSetAsync();
        }

        public async ValueTask DisposeAsync()
        {
            TryFetchData = false;

            if (RefreshTimer != null) await RefreshTimer.DisposeAsync();
        }

        protected async Task ReloadStatusPageConfigurationAsync()
        {
            while (TryFetchData)
            {
                //If any refresh timer is running stop it
                RefreshTimer?.Change(Timeout.Infinite, 0);

                Unauthorized = false;

                var credential = (await CredentialService.GetStatusPageCredentialsAsync()).FirstOrDefault(x => x.StatusPageId == StatusPageId);

                AccessToken = new("X-StatusPage-Access-Token", credential?.PasswordHash!);

                try
                {
                    var configurationResponse = await Http.SendAsync<StatusPageDto>(HttpMethod.Get, $"api/v1/StatusPages/public/{StatusPageId}", AccessToken, false, true);

                    if (configurationResponse == null) continue; //We were not able to get any (new) data. Retry until we do!

                    //On non default status pages use the id as soon as the name was resolved to keep refreshing even if the site is renamed
                    if (StatusPageId != "default") StatusPageId = configurationResponse.Id;

                    StatusPageConfiguration = configurationResponse;

                    //Start refresh interval
                    LastRefresh = DateTimeOffset.UtcNow;

                    RefreshSecondsRemaining = 60;

                    RefreshTimer = new(RefreshCountDownTickAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

                    RefreshInProgress = false;

                    //Push UI changes while more data is loaded
                    await InvokeAsync(StateHasChanged);

                    await ReloadIncidentsAsync();

                    await ReloadHistoriesAsync();

                    return; //Sucessfully refreshed the data. Time to exit the loop
                }
                catch (HttpRequestException ex)
                {
                    if (ex.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        LoginViewModel = new();

                        Unauthorized = true;

                        await InvokeAsync(StateHasChanged);

                        return;
                    }

                    if (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        NavigationManager.NavigateTo("/error/404", true);

                        return;
                    }
                }

                await InvokeAsync(StateHasChanged);

                await Task.Delay(1000);
            }
        }

        protected async Task ReloadIncidentsAsync()
        {
            if (StatusPageConfiguration == null) return;

            var request = new IncidentsForServicesRequest()
            {
                ServiceIds = GetAllServiceIds(),
                From = DateTimeOffset.Now.Date.ToUniversalTime().AddDays(-(StatusPageConfiguration.DaysIncidentTimeline ?? 0)),
                Until = DateTimeOffset.Now.Date.ToUniversalTime().AddDays(StatusPageConfiguration.DaysUpcomingMaintenances ?? 0)
            };

            var response = await Http.SendAsync<IncidentsForServicesRequest, IncidentsForServicesRequest.Response>(HttpMethod.Post, $"api/v1/Incidents/public/bulk", request, redirectToLeader: false);

            if (response == null) return;

            Incidents = response.Incidents;

            OngoingIncidents = Incidents
                .Where(x => (x.From <= DateTimeOffset.Now) && (!x.Until.HasValue || (DateTimeOffset.Now < x.Until)))
                .OrderBy(x => x.From)
                .ToList();

            CurrentMaxIncidentSeverity = OngoingIncidents.Count > 0 ? OngoingIncidents.SelectMany(x => x.Timeline).Max(x => x.Severity) : null;

            var from = DateTimeOffset.UtcNow;
            var until = from.AddDays(StatusPageConfiguration.DaysUpcomingMaintenances ?? 0);

            UpcomingMaintenances = Incidents
                .Where(x => x.Timeline.OrderBy(y => y.DateTime).Last().Severity == IncidentSeverity.Maintenance)
                .Where(x => x.From <= until && (!x.Until.HasValue || from <= x.Until.Value))
                .OrderBy(x => x.From)
                .ToList();

            await InvokeAsync(StateHasChanged);
        }

        protected async Task ReloadHistoriesAsync()
        {
            if (StatusPageConfiguration == null) return;

            var request = new ServiceStatusHistoryRequest()
            {
                ServiceIds = GetAllServiceIds(),
                From = DateTimeOffset.Now.Date.ToUniversalTime().AddDays(-StatusPageConfiguration.DaysStatusHistory),
                Until = DateTimeOffset.Now.Date.ToUniversalTime().AddDays(1) //Request includes everyhting until the end of the day
            };

            var response = await Http.SendAsync<ServiceStatusHistoryRequest, ServiceStatusHistoryRequest.Response>(HttpMethod.Post, $"api/v1/ServiceStatusHistories/public/bulk", request, redirectToLeader: false);

            if (response == null) return;

            ServiceStatusHistories = response.ServiceStatusHistories;

            if (ServiceStatusHistories != null)
            {
                var currentHistories = ServiceStatusHistories
                    .Where(x => DateTimeOffset.UtcNow.IsInRangeInclusiveNullable(x.From, x.Until))
                    .ToList();

                if (currentHistories.Count > 0)
                {
                    var outages = currentHistories
                        .SelectMany(x => x.Outages)
                        .Where(x => DateTimeOffset.UtcNow.IsInRangeInclusiveNullable(x.From, x.Until))
                        .ToList();

                    if (outages.Count > 0)
                    {
                        CurrentWorstServiceStatus = outages.Max(x => x.ServiceStatus);
                    }
                    else
                    {
                        //No outages that are happening right now but we had data
                        CurrentWorstServiceStatus = ServiceStatus.Available;
                    }
                }
                else
                {
                    CurrentWorstServiceStatus = ServiceStatus.Unknown;
                }
            }
            else
            {
                CurrentWorstServiceStatus = null;
            }

            await InvokeAsync(StateHasChanged);
        }

        protected List<string> GetAllServiceIds()
        {
            return StatusPageConfiguration.MonitorSummaries.SelectMany(x => x.LabeledMonitors).Select(x => x.MonitorId).Distinct().ToList();
        }

        protected List<IncidentDto> GetIncidentsForDayBack(int daysBack)
        {
            var dayUtc = DateTimeOffset.Now.Date.AddDays(-daysBack);

            return Incidents.Where(x =>
                x.From.Date <= dayUtc.Date &&       //Date we look at is at the same day incident happend or later
                (
                    !x.Until.HasValue ||            //The inciden has no end date yet, which means that at the time of request it was still ongoing  
                    x.Until.Value >= dayUtc.Date    //Our current day is not past the end date of the incident
                ))
                .ToList();
        }

        protected static string GetLocalDateString(int daysBack)
        {
            return DateTimeOffset.Now.Date.AddDays(-daysBack).ToLocalTime().ToString("d", CultureInfo.CurrentUICulture);
        }

        protected string GetDescriptionMarkUpString()
        {
            if (string.IsNullOrWhiteSpace(StatusPageConfiguration.Description)) return "";

            var value = StatusPageConfiguration.Description;

            var sanitizer = new HtmlSanitizer();

            //Strip off any not allowed html tags
            value = sanitizer.Sanitize(value);

            //Convert markdown to html
            value = Markdown.ToHtml(value, new MarkdownPipelineBuilder().UseSoftlineBreakAsHardlineBreak().Build());

            //Only return sanized content because it will be rendered as html dom elements - avoid script injections etc.
            return sanitizer.Sanitize(value);
        }

        protected async Task TriggerRefreshAsync()
        {
            RefreshInProgress = true;

            //Ui update to show refresh in progress
            await InvokeAsync(StateHasChanged);

            //Force an update
            await ReloadStatusPageConfigurationAsync();
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

        protected async Task SubmitLoginFormAsync()
        {
            var credentials = await CredentialService.GetStatusPageCredentialsAsync();

            //Remove existing ones
            credentials.RemoveAll(x => x.StatusPageId == StatusPageId);

            //Add new credentials using the latest password
            credentials.Add(new StatusPageCredentials()
            {
                StatusPageId = StatusPageId,
                PasswordHash = SHA256Hash.Create(LoginViewModel.Password)
            });

            //Submit new credential collection
            await CredentialService.SetStatusPageCredentialsAsync(credentials);

            //Wipe login mask for re-tries
            LoginViewModel.Password = "";

            //Try again to load the page now
            await ReloadStatusPageConfigurationAsync();
        }

        public class LoginModel
        {
            [Required(ErrorMessage = "Please enter the password to access this status page.")]
            public string Password { get; set; }
        }
    }
}