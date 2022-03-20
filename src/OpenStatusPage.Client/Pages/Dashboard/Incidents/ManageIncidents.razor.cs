using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Client.Extensions;
using OpenStatusPage.Shared.DataTransferObjects.Incidents;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Requests;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard.Incidents
{
    public partial class ManageIncidents
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

        protected List<IncidentMetaDto> IncidentMetas { get; set; }

        protected IncidentMetaDto SelectedIncident { get; set; }

        protected List<MonitorMetaDto> MonitorMetas { get; set; }

        protected IncidentViewModel IncidentModel { get; set; }

        protected bool TryFetchData { get; set; } = true;

        protected string SearchTerm { get; set; }

        protected bool SearchResolved { get; set; }

        protected MudForm EditForm { get; set; }

        public IncidentFluentValidator Validator { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            await RefreshIncidentsAsync();

            await base.OnInitializedAsync();
        }

        public async ValueTask DisposeAsync()
        {
            TryFetchData = false;
        }

        protected async Task RefreshIncidentsAsync()
        {
            while (TryFetchData)
            {
                //If we have no incidents yet, get them
                if (IncidentMetas == null)
                {
                    var response = await Http.SendAsync<List<IncidentMetaDto>>(HttpMethod.Get, "api/v1/Incidents", DashboardApiKeyHeader);

                    if (response == null)
                    {
                        await Task.Delay(1000);

                        continue;
                    }

                    IncidentMetas = response;
                }

                //Load the available monitors
                if (MonitorMetas == null)
                {
                    var response = await Http.SendAsync<List<MonitorMetaDto>>(HttpMethod.Get, "api/v1/Monitors", DashboardApiKeyHeader);

                    if (response == null)
                    {
                        await Task.Delay(1000);

                        continue;
                    }

                    MonitorMetas = response;
                }

                break;
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnItemSelectedAsync(object selection)
        {
            if (selection is not IncidentMetaDto selectedMetaData || SelectedIncident == selectedMetaData) return;

            await RefreshModelAsync(selectedMetaData);

            //If we can not build the model (server might be unavailable) deselect again.
            if (IncidentModel == null)
            {
                SelectedIncident = null!;
                return;
            }

            SelectedIncident = selectedMetaData;
        }

        public async Task RefreshModelAsync(IncidentMetaDto selectedMetaData)
        {
            var newModel = Mapper.Map<IncidentViewModel>(await GetIncidentDataAsync(selectedMetaData));

            if (newModel == null) return;

            IncidentModel = newModel;

            //Refresh data onto list dtos if loaded from api
            if (!string.IsNullOrEmpty(IncidentModel.Id))
            {
                var lastTimelineItem = IncidentModel.Timeline.OrderBy(x => x.DateTime).LastOrDefault();

                selectedMetaData.Id = IncidentModel.Id;
                selectedMetaData.Name = IncidentModel.Name;
                selectedMetaData.LatestStatus = lastTimelineItem?.Status ?? IncidentStatus.Created;
                selectedMetaData.LatestSeverity = lastTimelineItem?.Severity ?? IncidentSeverity.Information;
            }

            //Create new timeline items to submit as change if they will differ from original status
            IncidentModel.IncidentChangeitem = new()
            {
                Severity = selectedMetaData.LatestSeverity,
                Status = selectedMetaData.LatestStatus
            };

            IncidentModel.MaintenanceFromDate = IncidentModel.From.LocalDateTime.Date;
            IncidentModel.MaintenanceFromTime = IncidentModel.From.LocalDateTime.TimeOfDay;
            IncidentModel.MaintenanceUntilDate = IncidentModel.Until?.LocalDateTime.Date;
            IncidentModel.MaintenanceUntilTime = IncidentModel.Until?.LocalDateTime.TimeOfDay;
        }

        public async Task<IncidentDto> GetIncidentDataAsync(IncidentMetaDto metaDto)
        {
            //Newly created instance, yet to first submit
            if (string.IsNullOrWhiteSpace(metaDto.Id)) return new IncidentDto
            {
                Name = metaDto.Name,
                From = DateTimeOffset.UtcNow,
                AffectedServices = new(),
                Timeline = new(),
            };

            var response = await Http.SendAsync<IncidentDto>(HttpMethod.Get, $"api/v1/Incidents/{metaDto.Id}", DashboardApiKeyHeader);

            if (response == null) return null;

            //Init list if it was still empty
            response.AffectedServices ??= new();
            response.Timeline ??= new();

            return response;
        }

        protected async Task SubmitChangeAsync()
        {
            await EditForm.Validate();

            if (!EditForm.IsValid) return;

            var sendContainer = Mapper.Map<IncidentDto>(IncidentModel);

            //Update version
            sendContainer.Version++;

            //Refresh submission timestamp
            IncidentModel.IncidentChangeitem.DateTime = DateTimeOffset.UtcNow;
            sendContainer.Timeline.Add(IncidentModel.IncidentChangeitem);

            //Update version of the last timeline item
            sendContainer.Timeline.Last().Version++;

            if (IncidentModel.IncidentChangeitem.Severity == IncidentSeverity.Maintenance)
            {
                sendContainer.From = IncidentModel.MaintenanceFromDate!.Value.Add(IncidentModel.MaintenanceFromTime!.Value).ToUniversalTime();
                sendContainer.Until = IncidentModel.MaintenanceUntilDate?.Add(IncidentModel.MaintenanceUntilTime!.Value).ToUniversalTime();

                if (sendContainer.Until.HasValue && (sendContainer.Until.Value <= sendContainer.From))
                {
                    sendContainer.Until = sendContainer.From.AddMinutes(1);
                }
            }

            if (IncidentModel.IncidentChangeitem.Status == IncidentStatus.Resolved)
            {
                sendContainer.Until = DateTimeOffset.UtcNow;
            }

            //Submit with proposed id if it does't already have one, so the request replication is deterministic
            sendContainer.Id ??= Guid.NewGuid().ToString();

            foreach (var timelineItem in sendContainer.Timeline)
            {
                timelineItem.Id ??= Guid.NewGuid().ToString();
            }

            var response = await Http.SendAsync<SuccessResponse>(
                HttpMethod.Post,
                $"api/v1/Incidents",
                sendContainer,
                DashboardApiKeyHeader);

            if (response != null && response.WasSuccessful)
            {
                //Create/Update was successful, so update the meta data id for the model refresh call.
                SelectedIncident.Id = sendContainer.Id;

                //Rebuild view model with new data
                await RefreshModelAsync(SelectedIncident);

                Snackbar.Add("Changes saved successfully", MudBlazor.Severity.Success);
            }
            else
            {
                //Create/Update failed, so refresh the server data if the entity already existed
                if (!string.IsNullOrEmpty(SelectedIncident.Id))
                {
                    //Rebuild view model with new data
                    await RefreshModelAsync(SelectedIncident);
                }

                Snackbar.Add("Changes could not be saved", MudBlazor.Severity.Error);
            }

            //Deselect an incident once it has been resolved and user does not want to show resolved inicents
            if (SelectedIncident != null && !SearchResolved && SelectedIncident.LatestStatus == IncidentStatus.Resolved)
            {
                SelectedIncident = null!;
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task RemoveIncidentAsync()
        {
            if (await DialogService.ConfirmAsync(
                $"Delete incident {SelectedIncident.Name}",
                $"Are you sure you want to delete the incident {SelectedIncident.Name}?",
                "Delete",
                submitColor: Color.Error,
                confirmIcon: Icons.Outlined.DeleteForever))
            {

                //No id means it was not persistent yet
                bool success = string.IsNullOrEmpty(SelectedIncident.Id);

                if (!success)
                {
                    var response = await Http.SendAsync<SuccessResponse>(HttpMethod.Delete, $"api/v1/Incidents/{SelectedIncident.Id}", DashboardApiKeyHeader);

                    success = response != null && response.WasSuccessful;
                }

                if (success)
                {
                    //Remove instance
                    IncidentMetas.Remove(SelectedIncident);

                    //Deselect provider
                    SelectedIncident = null!;

                    Snackbar.Add("Incident deleted successfully", MudBlazor.Severity.Success);
                }
                else
                {
                    Snackbar.Add("There was a problem deleting the incident", MudBlazor.Severity.Error);
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task OpenAddIncidentAsync()
        {
            var result = await DialogService.Show<AddIncidentDialog>("Add a new incident", new DialogOptions()
            {
                FullWidth = true,
                CloseOnEscapeKey = true,
                CloseButton = true,
                MaxWidth = MaxWidth.Small
            }).Result;

            if (result.Cancelled || result.Data is not IncidentMetaDto newIncident) return;

            IncidentMetas.Add(newIncident);

            await OnItemSelectedAsync(newIncident);
        }

        protected static string IncidentStatusString(IncidentStatus status)
        {
            return status switch
            {
                IncidentStatus.Created => "Created",
                IncidentStatus.Acknowledged => "Acknowledged",
                IncidentStatus.Investigating => "Investigating",
                IncidentStatus.Monitoring => "Monitoring",
                IncidentStatus.Resolved => "Resolved",
                _ => "Unknown",
            };
        }

        protected static string IncidentSeverityString(IncidentSeverity severity)
        {
            return severity switch
            {
                IncidentSeverity.Information => "Information",
                IncidentSeverity.Maintenance => "Maintenance",
                IncidentSeverity.Minor => "Minor",
                IncidentSeverity.Major => "Major",
                _ => "Unknown",
            };
        }

        protected static Color IncidentSeverityColor(IncidentSeverity severity)
        {
            return severity switch
            {
                IncidentSeverity.Information => Color.Info,
                IncidentSeverity.Maintenance => Color.Primary,
                IncidentSeverity.Minor => Color.Warning,
                IncidentSeverity.Major => Color.Error,
                _ => Color.Default,
            };
        }

        protected string GetMonitorDisplayName(string monitorId)
        {
            return MonitorMetas.FirstOrDefault(x => x.Id == monitorId)?.Name ?? "Unknown";
        }

        protected async Task<IEnumerable<MonitorMetaDto>> SearchMonitorAsync(string value)
        {
            var possible = MonitorMetas.Where(x => !IncidentModel.AffectedServices.Contains(x.Id)).ToList();

            // if text is null or empty, show complete list
            if (string.IsNullOrEmpty(value)) return possible;

            return possible.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        public class IncidentViewModel : IncidentDto
        {
            public IncidentTimelineItem IncidentChangeitem { get; set; }

            public DateTime? MaintenanceFromDate { get; set; }

            public TimeSpan? MaintenanceFromTime { get; set; }

            public DateTime? MaintenanceUntilDate { get; set; }

            public TimeSpan? MaintenanceUntilTime { get; set; }
        }

        public class DtoMapper : Profile
        {
            public DtoMapper()
            {
                CreateMap<IncidentDto, IncidentViewModel>().ReverseMap();
            }
        }

        public class IncidentFluentValidator : AbstractValidator<IncidentViewModel>
        {
            public IncidentFluentValidator()
            {
                When(x => x.IncidentChangeitem.Severity == IncidentSeverity.Maintenance, () =>
                {
                    RuleFor(x => x.MaintenanceFromDate)
                        .NotNull()
                        .WithMessage("This field is required");

                    RuleFor(x => x.MaintenanceFromTime)
                        .NotNull()
                        .WithMessage("This field is required");

                    RuleFor(x => x.MaintenanceUntilDate)
                        .GreaterThanOrEqualTo(x => x.MaintenanceFromDate!.Value)
                        .WithMessage("End date must be equal or greater than start date");

                    RuleFor(x => x.MaintenanceUntilTime)
                        .GreaterThan(x => x.MaintenanceFromTime!.Value)
                        .WithMessage("End time must be greater than start time");
                });
            }

            public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
            {
                var result = await ValidateAsync(ValidationContext<IncidentViewModel>.CreateWithOptions((IncidentViewModel)model, x => x.IncludeProperties(propertyName)));
                if (result.IsValid)
                    return Array.Empty<string>();
                return result.Errors.Select(e => e.ErrorMessage);
            };
        }
    }
}
