using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Client.Extensions;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.StatusPages;
using OpenStatusPage.Shared.Requests;
using static OpenStatusPage.Client.Application.TransparentHttpClient;
using static OpenStatusPage.Shared.DataTransferObjects.StatusPages.StatusPageDto;
using static OpenStatusPage.Shared.DataTransferObjects.StatusPages.StatusPageDto.MonitorSummary;

namespace OpenStatusPage.Client.Pages.Dashboard.StatusPages
{
    public partial class ManageStatusPages : IAsyncDisposable
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

        protected List<StatusPageMetaDto> StatusPageMetaData { get; set; }

        protected List<MonitorMetaDto> MonitorMetas { get; set; }

        protected StatusPageMetaDto SelectedStatusPage { get; set; }

        protected StatusPageViewModel StatusPageModel { get; set; }

        public StatusPageFluentValidator Validator { get; set; } = new();

        protected bool TryFetchData { get; set; } = true;

        protected string SearchTerm { get; set; }

        protected MudForm EditForm { get; set; }

        protected bool _showPassword;

        protected InputType _passwordInput = InputType.Password;

        protected string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

        protected async Task PasswordToggleShowAsync()
        {
            if (_showPassword)
            {
                _showPassword = false;
                _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
                _passwordInput = InputType.Password;
            }
            else
            {
                _showPassword = true;
                _passwordInputIcon = Icons.Material.Filled.Visibility;
                _passwordInput = InputType.Text;
            }

            await InvokeAsync(StateHasChanged);
        }

        protected override async Task OnInitializedAsync()
        {
            await RefreshStatusPagesAsync();

            await base.OnInitializedAsync();
        }

        public async ValueTask DisposeAsync()
        {
            TryFetchData = false;
        }

        protected async Task RefreshStatusPagesAsync()
        {
            while (TryFetchData)
            {
                //If we have no status pages yet, get them
                if (StatusPageMetaData == null)
                {
                    var response = await Http.SendAsync<List<StatusPageMetaDto>>(HttpMethod.Get, "api/v1/StatusPages", DashboardApiKeyHeader);

                    if (response == null)
                    {
                        await Task.Delay(1000);

                        continue;
                    }

                    StatusPageMetaData = response;
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
            if (selection is not StatusPageMetaDto selectedMetaData || SelectedStatusPage == selectedMetaData) return;

            await RefreshModelAsync(selectedMetaData);

            //If we can not build the model (server might be unavailable) deselect again.
            if (StatusPageModel == null)
            {
                SelectedStatusPage = null!;
                return;
            }

            SelectedStatusPage = selectedMetaData;
        }

        protected async Task RefreshModelAsync(StatusPageMetaDto selectedMetaData)
        {
            var newModel = Mapper.Map<StatusPageViewModel>(await LoadDataToModelAsync(selectedMetaData));

            if (newModel == null) return;

            StatusPageModel = newModel;

            selectedMetaData.Id = StatusPageModel.Id;
            selectedMetaData.Name = StatusPageModel.Name;
        }

        public async Task<StatusPageConfigurationDto> LoadDataToModelAsync(StatusPageMetaDto metaDto)
        {
            //Newly created instance, yet to first submit
            if (string.IsNullOrWhiteSpace(metaDto.Id)) return new StatusPageConfigurationDto
            {
                Name = metaDto.Name,
                DaysStatusHistory = 7,
                MonitorSummaries = new()
            };

            var response = await Http.SendAsync<StatusPageConfigurationDto>(HttpMethod.Get, $"api/v1/StatusPages/{metaDto.Id}", DashboardApiKeyHeader);

            if (response == null) return null;

            //Init list if it was still empty
            response.MonitorSummaries ??= new();

            return response;
        }

        protected async Task SubmitChangeAsync()
        {
            await EditForm.Validate();

            if (StatusPageMetaData.Any(x =>
                !string.IsNullOrEmpty(x.Id) &&
                x.Id != StatusPageModel.Id &&
                x.Name.ToLowerInvariant().Equals(StatusPageModel.Name.ToLowerInvariant())))
            {
                Snackbar.Add("A status page with the same name already exists.", MudBlazor.Severity.Error);
                return;
            }

            if (!EditForm.IsValid) return;

            var sendContainer = Mapper.Map<StatusPageConfigurationDto>(StatusPageModel);

            //Submit with proposed id if it does't already have one, so the request replication is deterministic
            sendContainer.Id ??= Guid.NewGuid().ToString();

            //Update version
            sendContainer.Version++;

            sendContainer.MonitorSummaries.ForEach(summary =>
            {
                summary.Id ??= Guid.NewGuid().ToString();
                summary.StatusPageId = sendContainer.Id;
                summary.Version++;

                summary.LabeledMonitors.ForEach(labeledMonitor =>
                {
                    labeledMonitor.Id ??= Guid.NewGuid().ToString();
                    labeledMonitor.MonitorSummaryId = summary.Id;
                    labeledMonitor.Version++;
                });
            });

            var response = await Http.SendAsync<SuccessResponse>(
                HttpMethod.Post,
                $"api/v1/StatusPages",
                sendContainer,
                DashboardApiKeyHeader);

            if (response != null && response.WasSuccessful)
            {
                //Create/Update was successful, so update the meta data id for the model refresh call.
                SelectedStatusPage.Id = sendContainer.Id;

                //Rebuild view model with new data
                await RefreshModelAsync(SelectedStatusPage);

                Snackbar.Add("Changes saved successfully", MudBlazor.Severity.Success);
            }
            else
            {
                //Create/Update failed, so refresh the server data if the entity already existed
                if (!string.IsNullOrEmpty(SelectedStatusPage.Id))
                {
                    //Rebuild view model with new data
                    await RefreshModelAsync(SelectedStatusPage);
                }

                Snackbar.Add("Changes could not be saved", MudBlazor.Severity.Error);
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task RemoveStatusPageAsync()
        {
            if (await DialogService.ConfirmAsync(
                $"Delete status page {SelectedStatusPage.Name}",
                $"Are you sure you want to delete the status page {SelectedStatusPage.Name}?",
                "Delete",
                submitColor: Color.Error,
                confirmIcon: Icons.Outlined.DeleteForever))
            {

                //No id means it was not persistent yet
                bool success = string.IsNullOrEmpty(SelectedStatusPage.Id);

                if (!success)
                {
                    var response = await Http.SendAsync<SuccessResponse>(HttpMethod.Delete, $"api/v1/StatusPages/{SelectedStatusPage.Id}", DashboardApiKeyHeader);

                    success = response != null && response.WasSuccessful;
                }

                if (success)
                {
                    //Remove instance
                    StatusPageMetaData.Remove(SelectedStatusPage);

                    //Deselect provider
                    SelectedStatusPage = null!;

                    await InvokeAsync(StateHasChanged);

                    Snackbar.Add("Status page deleted successfully", MudBlazor.Severity.Success);
                }
                else
                {
                    Snackbar.Add("There was a problem deleting the status page", MudBlazor.Severity.Error);
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task OpenAddMonitorAsync()
        {
            var result = await DialogService.Show<AddStatusPageDialog>("Add a new status page", new DialogOptions()
            {
                FullWidth = true,
                CloseOnEscapeKey = true,
                CloseButton = true,
                MaxWidth = MaxWidth.Small
            }).Result;

            if (result.Cancelled || result.Data is not StatusPageMetaDto newStatusPage) return;

            if (StatusPageMetaData.Any(x => x.Name.ToLowerInvariant().Equals(newStatusPage.Name.ToLowerInvariant())))
            {
                Snackbar.Add("A status page with the same name already exists.", MudBlazor.Severity.Error);
                return;
            }

            StatusPageMetaData.Add(newStatusPage);

            await OnItemSelectedAsync(newStatusPage);
        }

        protected void AddGroup()
        {
            var newOrderIndex = StatusPageModel.MonitorSummaries.Count > 0 ? StatusPageModel.MonitorSummaries.Max(x => x.OrderIndex) + 1 : 0;

            StatusPageModel.MonitorSummaries.Add(new()
            {
                OrderIndex = newOrderIndex,
                LabeledMonitors = new()
            });
        }

        protected void RemoveGroup(MonitorSummary summary)
        {
            StatusPageModel.MonitorSummaries.Remove(summary);

            //Close the removal gap
            foreach (var monitorSummary in StatusPageModel.MonitorSummaries)
            {
                if (monitorSummary.OrderIndex > summary.OrderIndex) monitorSummary.OrderIndex--;
            }
        }

        protected void MoveGroupUp(MonitorSummary summary)
        {
            var moveToIndex = summary.OrderIndex - 1;

            var swapGroup = StatusPageModel.MonitorSummaries.FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swapGroup == null) return;

            swapGroup.OrderIndex = summary.OrderIndex;

            summary.OrderIndex = moveToIndex;
        }

        protected void MoveGroupDown(MonitorSummary summary)
        {
            var moveToIndex = summary.OrderIndex + 1;

            var swapSummary = StatusPageModel.MonitorSummaries.FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swapSummary == null) return;

            swapSummary.OrderIndex = summary.OrderIndex;

            summary.OrderIndex = moveToIndex;
        }

        protected static void AddMonitorToGroup(MonitorMetaDto monitor, MonitorSummary summary)
        {
            if (monitor == null) return;

            var newOrderIndex = summary.LabeledMonitors.Count > 0 ? summary.LabeledMonitors.Max(x => x.OrderIndex) + 1 : 0;

            summary.LabeledMonitors.Add(new()
            {
                OrderIndex = newOrderIndex,
                MonitorId = monitor.Id,
                Label = monitor.Name
            });
        }

        protected static void RemoveMonitorFromGroup(LabeledMonitor monitor, MonitorSummary summary)
        {
            if (monitor == null) return;

            summary.LabeledMonitors.Remove(monitor);

            foreach (var labeledMonitor in summary.LabeledMonitors)
            {
                if (labeledMonitor.OrderIndex > monitor.OrderIndex) labeledMonitor.OrderIndex--;
            }
        }

        protected static void MoveMonitorUp(LabeledMonitor monitor, MonitorSummary summary)
        {
            var moveToIndex = monitor.OrderIndex - 1;

            var swap = summary.LabeledMonitors.FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swap == null) return;

            swap.OrderIndex = monitor.OrderIndex;

            monitor.OrderIndex = moveToIndex;
        }

        protected static void MoveMonitorDown(LabeledMonitor monitor, MonitorSummary summary)
        {
            var moveToIndex = monitor.OrderIndex + 1;

            var swap = summary.LabeledMonitors.FirstOrDefault(x => x.OrderIndex == moveToIndex);

            if (swap == null) return;

            swap.OrderIndex = monitor.OrderIndex;

            monitor.OrderIndex = moveToIndex;
        }

        protected async Task<IEnumerable<MonitorMetaDto>> SearchMonitorAsync(string value, MonitorSummary summary)
        {
            var possibleMetas = MonitorMetas.Where(x => !summary.LabeledMonitors.Any(y => y.MonitorId == x.Id)).ToList();

            // if text is null or empty, show complete list
            if (string.IsNullOrEmpty(value)) return possibleMetas;

            return possibleMetas.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        protected string GetMonitorName(LabeledMonitor monitor)
        {
            return MonitorMetas.Where(x => x.Id == monitor.MonitorId).FirstOrDefault()?.Name ?? "Unknown";
        }

        protected string MetaToTypeString(LabeledMonitor monitor)
        {
            return MetaToTypeString(MonitorMetas.Where(x => x.Id == monitor.MonitorId).FirstOrDefault()!);
        }

        protected static string MetaToTypeString(MonitorMetaDto monitor)
        {
            return monitor?.Type.ToLowerInvariant() switch
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

        public class StatusPageViewModel : StatusPageConfigurationDto
        {
        }

        public class DtoMapper : Profile
        {
            public DtoMapper()
            {
                CreateMap<StatusPageConfigurationDto, StatusPageViewModel>().ReverseMap();
            }
        }

        public class StatusPageFluentValidator : AbstractValidator<StatusPageConfigurationDto>
        {
            public StatusPageFluentValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty()
                        .WithMessage("This field is required.")
                    .Must(x => x.All(c => char.IsLetterOrDigit(c) || c == '-'))
                        .WithMessage("Only a-zA-Z0-9 and '-' are allowed.")
                    .Must(x => x.ToLowerInvariant() != "default")
                        .WithMessage("Name default is reserved.")
                    .Must(x => x.ToLowerInvariant() != "dashboard")
                        .WithMessage("Name dashboard is reserved.");

                When(x => x.EnableUpcomingMaintenances, () =>
                {
                    RuleFor(x => x.DaysUpcomingMaintenances)
                    .NotNull()
                        .WithMessage("This field is required.")
                    .InclusiveBetween(1, 90)
                        .WithMessage("Invalid value. 1-90 allowed.");
                });

                When(x => x.EnableIncidentTimeline, () =>
                {
                    RuleFor(x => x.DaysIncidentTimeline)
                    .NotNull()
                        .WithMessage("This field is required.")
                    .InclusiveBetween(1, 90)
                        .WithMessage("Invalid value. 1-90 allowed.");
                });

                RuleFor(x => x.DaysStatusHistory)
                    .InclusiveBetween(1, 90)
                    .WithMessage("Invalid value. 1-90 allowed.");
            }

            public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
            {
                var result = await ValidateAsync(ValidationContext<StatusPageConfigurationDto>.CreateWithOptions((StatusPageConfigurationDto)model, x => x.IncludeProperties(propertyName)));
                if (result.IsValid)
                    return Array.Empty<string>();
                return result.Errors.Select(e => e.ErrorMessage);
            };
        }
    }
}
