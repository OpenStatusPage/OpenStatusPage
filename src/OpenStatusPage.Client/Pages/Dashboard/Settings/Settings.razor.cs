using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Client.Application.Authentication;
using OpenStatusPage.Shared.DataTransferObjects;
using OpenStatusPage.Shared.DataTransferObjects.Configuration;
using OpenStatusPage.Shared.DataTransferObjects.StatusPages;
using OpenStatusPage.Shared.Requests;
using System.ComponentModel.DataAnnotations;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard.Settings
{
    public partial class Settings : IAsyncDisposable
    {
        [Inject]
        public CredentialService CredentialService { get; set; }

        [Inject]
        public TransparentHttpClient Http { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [CascadingParameter]
        protected HeaderEntry DashboardApiKeyHeader { get; set; }

        public ApplicationSettingsDto ApplicationSettings { get; set; }

        public List<StatusPageMetaDto> StatusPageMetas { get; set; }

        protected bool TryFetchData { get; set; } = true;

        protected ApplicationSettingsModel ApplicationSettingsViewModel { get; set; }

        protected EditContext AppSettingsContext { get; set; }

        protected override async Task OnInitializedAsync()
        {


            await ReloadSettingsAsync();

            await base.OnInitializedAsync();
        }

        public async ValueTask DisposeAsync()
        {
            TryFetchData = false;
        }

        protected async Task ReloadSettingsAsync()
        {
            while (TryFetchData)
            {
                //If we have no appliction settings yet, get them
                if (ApplicationSettings == null)
                {
                    var response = await Http.SendAsync<ApplicationSettingsDto>(HttpMethod.Get, "api/v1/ApplicationSettings", DashboardApiKeyHeader);

                    if (response == null)
                    {
                        await Task.Delay(1000);

                        continue;
                    }

                    ApplicationSettings = response;
                }

                //If we have the list of status pages, get them
                if (StatusPageMetas == null)
                {
                    var response = await Http.SendAsync<List<StatusPageMetaDto>>(HttpMethod.Get, "api/v1/StatusPages", DashboardApiKeyHeader);

                    if (response == null)
                    {
                        await Task.Delay(1000);

                        continue;
                    }

                    StatusPageMetas = response;
                }

                RebuildViewModel();

                break;
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task SubmitAppsettingsChangeAsync()
        {
            if (!AppSettingsContext.Validate()) return;

            var data = new ApplicationSettingsDto
            {
                Id = ApplicationSettingsViewModel.Id,
                Version = ApplicationSettingsViewModel.Version + 1,
                DefaultStatusPageId = ApplicationSettingsViewModel.DefaultStatusPage.Id,
                DaysMonitorHistory = ApplicationSettingsViewModel.DaysMonitorHistory,
                DaysIncidentHistory = ApplicationSettingsViewModel.DaysIncidentHistory,
                StatusFlushInterval = ApplicationSettingsViewModel.StatusFlushInterval
            };

            var response = await Http.SendAsync<SuccessResponse>(HttpMethod.Post, "api/v1/ApplicationSettings", data, DashboardApiKeyHeader);

            if (response != null && response.WasSuccessful)
            {
                Snackbar.Add("Changes were saved successfully", Severity.Success);
            }
            else
            {
                Snackbar.Add("There was a problem saving the data", Severity.Error);
            }

            //Force refetch of latest data
            ApplicationSettings = null!;
            await ReloadSettingsAsync();
        }

        protected void RebuildViewModel()
        {
            ApplicationSettingsViewModel = new()
            {
                Id = ApplicationSettings.Id,
                Version = ApplicationSettings.Version,
                DefaultStatusPage = StatusPageMetas.First(x => x.Id == ApplicationSettings.DefaultStatusPageId),
                DaysIncidentHistory = ApplicationSettings.DaysIncidentHistory,
                DaysMonitorHistory = ApplicationSettings.DaysMonitorHistory,
                StatusFlushInterval = ApplicationSettings.StatusFlushInterval
            };

            AppSettingsContext = new(ApplicationSettingsViewModel);
        }

        private async Task<IEnumerable<StatusPageMetaDto>> SearchStatusPageAsync(string value)
        {
            // if text is null or empty, show complete list
            if (string.IsNullOrEmpty(value)) return StatusPageMetas;

            return StatusPageMetas.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        public class ApplicationSettingsModel : EntityBaseDto
        {
            [Required(ErrorMessage = "This field is required")]
            public StatusPageMetaDto DefaultStatusPage { get; set; }

            [Required(ErrorMessage = "This field is required")]
            [Range(0, 90, ErrorMessage = "Invalid range. Only 0-90 allowed")]
            public ushort DaysMonitorHistory { get; set; }

            [Required(ErrorMessage = "This field is required")]
            [Range(0, 90, ErrorMessage = "Invalid range. Only 0-90 allowed")]
            public ushort DaysIncidentHistory { get; set; }

            private ulong _statusFlushIntervalField;

            [Required(ErrorMessage = "This field is required")]
            public ulong StatusFlushIntervalField
            {
                get => _statusFlushIntervalField;
                set => _statusFlushIntervalField = value;
            }

            public ulong StatusFlushIntervalMuliplier { get; set; } = 1;

            public TimeSpan StatusFlushInterval
            {
                get => TimeSpan.FromSeconds(_statusFlushIntervalField * StatusFlushIntervalMuliplier);
                set
                {
                    if (value.TotalDays >= 1.0)
                    {
                        _statusFlushIntervalField = (ulong)(value.TotalSeconds / 86400);
                        StatusFlushIntervalMuliplier = 86400;
                    }
                    else if (value.TotalHours >= 1.0)
                    {
                        _statusFlushIntervalField = (ulong)(value.TotalSeconds / 3600);
                        StatusFlushIntervalMuliplier = 3600;
                    }
                    else if (value.TotalMinutes >= 1.0)
                    {
                        _statusFlushIntervalField = (ulong)(value.TotalSeconds / 60);
                        StatusFlushIntervalMuliplier = 60;
                    }
                    else
                    {
                        _statusFlushIntervalField = (ulong)(value.TotalSeconds);
                        StatusFlushIntervalMuliplier = 1;
                    }
                }
            }
        }
    }
}