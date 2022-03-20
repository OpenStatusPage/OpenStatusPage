using Microsoft.AspNetCore.Components;
using MudBlazor;
using OpenStatusPage.Shared.DataTransferObjects.StatusPages;

namespace OpenStatusPage.Client.Pages.Dashboard.StatusPages
{
    public partial class AddStatusPageDialog
    {
        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        public string Name { get; set; }

        private void AddProviderAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                MudDialog.Close(DialogResult.Cancel());
            }
            else
            {
                MudDialog.Close(DialogResult.Ok(new StatusPageMetaDto
                {
                    Name = Name
                }));
            }
        }
    }
}