using Microsoft.AspNetCore.Components;
using MudBlazor;
using OpenStatusPage.Shared.DataTransferObjects.NotificationProviders;

namespace OpenStatusPage.Client.Pages.Dashboard.Settings.Notifications
{
    public partial class AddNotificationProviderDialog
    {
        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        public string SelectedName { get; set; }

        public string SelectedValue { get; set; }

        private void AddProviderAsync()
        {
            NotificationProviderMetaDto? type = SelectedValue switch
            {
                "Webhook" => new()
                {
                    Type = "WebhookProvider"
                },
                "SMTP Email" => new()
                {
                    Type = "SmtpEmailProvider"
                },
                _ => null
            };

            if (type != null) type.Name = SelectedName;

            MudDialog.Close(DialogResult.Ok(type));
        }
    }
}