using AutoMapper;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Client.Extensions;
using OpenStatusPage.Client.Pages.Dashboard.Settings.Notifications.Providers;
using OpenStatusPage.Shared.DataTransferObjects.NotificationProviders;
using OpenStatusPage.Shared.Requests;
using static OpenStatusPage.Client.Application.TransparentHttpClient;
using static OpenStatusPage.Client.Pages.Dashboard.Settings.Notifications.Providers.NotificationProvider;
using static OpenStatusPage.Client.Pages.Dashboard.Settings.Notifications.Providers.SmtpEmailProvider;
using static OpenStatusPage.Client.Pages.Dashboard.Settings.Notifications.Providers.WebhookProvider;

namespace OpenStatusPage.Client.Pages.Dashboard.Settings.Notifications
{
    public partial class ManageNotifications : IAsyncDisposable
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

        protected List<NotificationProviderMetaDto> ProviderMetadata { get; set; }

        protected NotificationProviderMetaDto SelectedProvider { get; set; }

        protected ProviderViewModel ProviderModel { get; set; }

        protected bool TryFetchData { get; set; } = true;

        protected string SearchTerm { get; set; }

        protected MudForm EditForm { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await RefreshNotificationProvidersAsync();

            await base.OnInitializedAsync();
        }

        public async ValueTask DisposeAsync()
        {
            TryFetchData = false;
        }

        protected async Task RefreshNotificationProvidersAsync()
        {
            while (TryFetchData)
            {
                //If we have no notification providers yet, get them
                if (ProviderMetadata == null)
                {
                    var response = await Http.SendAsync<List<NotificationProviderMetaDto>>(HttpMethod.Get, "api/v1/NotificationProviders", DashboardApiKeyHeader);

                    if (response == null)
                    {
                        await Task.Delay(1000);

                        continue;
                    }

                    ProviderMetadata = response;

                    break;
                }

            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnProviderSelectedAsync(object newValue)
        {
            if (newValue is not NotificationProviderMetaDto selectedMetaData || SelectedProvider == selectedMetaData) return;

            await RefreshModelAsync(selectedMetaData);

            //If we can not build the provider model (server might be unavailable) deselect again.
            if (ProviderModel == null)
            {
                SelectedProvider = null!;
                return;
            }

            SelectedProvider = selectedMetaData;
        }

        protected async Task RefreshModelAsync(NotificationProviderMetaDto selectedMetaData)
        {
            var newModel = selectedMetaData.Type.ToLowerInvariant() switch
            {
                "webhookprovider" => await WebhookProvider.LoadDataToModelAsync(Http, DashboardApiKeyHeader, Mapper, selectedMetaData),
                "smtpemailprovider" => await SmtpEmailProvider.LoadDataToModelAsync(Http, DashboardApiKeyHeader, Mapper, selectedMetaData),
                _ => null!,
            };

            if (newModel == null) return;

            ProviderModel = newModel;

            selectedMetaData.Id = ProviderModel.Id;
            selectedMetaData.Name = ProviderModel.Name;
        }

        protected async Task SubmitChangeAsync()
        {
            await EditForm.Validate();

            if (!EditForm.IsValid) return;

            NotificationProviderDto sendContainer = ProviderModel switch
            {
                WebhookProviderViewModel => new WebhookProviderDto(),
                SmtpEmailProviderViewModel => new SmtpEmailProviderDto(),
                _ => null!,
            };

            if (sendContainer == null) return;

            //Apply changes from view model onto selected instance
            Mapper.Map(ProviderModel, sendContainer);

            //Submit with proposed id if it does't already have one, so the request replication is deterministic
            sendContainer.Id ??= Guid.NewGuid().ToString();

            //Increase version
            sendContainer.Version++;

            var response = await Http.SendAsync<SuccessResponse>(
                HttpMethod.Post,
                $"api/v1/NotificationProviders?typename={sendContainer.GetType().Name}",
                sendContainer,
                DashboardApiKeyHeader);

            if (response != null && response.WasSuccessful)
            {
                //Create/Update was successful, so update the meta data id for the model refresh call.
                SelectedProvider.Id = sendContainer.Id;

                //Rebuild view model with new data
                await RefreshModelAsync(SelectedProvider);

                Snackbar.Add("Changes saved successfully", Severity.Success);
            }
            else
            {
                //Create/Update failed, so refresh the server data if the entity already existed
                if (!string.IsNullOrEmpty(SelectedProvider.Id))
                {
                    //Rebuild view model with new data
                    await RefreshModelAsync(SelectedProvider);
                }

                Snackbar.Add("Changes could not be saved", Severity.Error);
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task RemoveProviderAsync()
        {
            if (await DialogService.ConfirmAsync(
                $"Delete notification provider {SelectedProvider.Name}",
                $"Are you sure you want to delete the notification provider {SelectedProvider.Name}?",
                "Delete",
                submitColor: Color.Error,
                confirmIcon: Icons.Outlined.DeleteForever))
            {

                //No id means it was not persistent yet
                bool success = string.IsNullOrEmpty(SelectedProvider.Id);

                if (!success)
                {
                    var response = await Http.SendAsync<SuccessResponse>(HttpMethod.Delete, $"api/v1/NotificationProviders/{SelectedProvider.Id}", DashboardApiKeyHeader);

                    success = response != null && response.WasSuccessful;
                }

                if (success)
                {
                    //Remove instance
                    ProviderMetadata.Remove(SelectedProvider);

                    //Deselect provider
                    SelectedProvider = null!;

                    await InvokeAsync(StateHasChanged);

                    Snackbar.Add("Notification provider deleted successfully", Severity.Success);
                }
                else
                {
                    Snackbar.Add("There was a problem deleting the notification provider", Severity.Error);
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task OpenAddProviderAsync()
        {
            var result = await DialogService.Show<AddNotificationProviderDialog>("Add a new notifiation provider", new DialogOptions()
            {
                FullWidth = true,
                CloseOnEscapeKey = true,
                CloseButton = true,
                MaxWidth = MaxWidth.Small
            }).Result;

            if (result.Cancelled || result.Data is not NotificationProviderMetaDto newProvider) return;

            ProviderMetadata.Add(newProvider);

            await OnProviderSelectedAsync(newProvider);
        }
    }
}