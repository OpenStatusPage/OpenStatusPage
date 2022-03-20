using Microsoft.AspNetCore.Components;
using MudBlazor;
using OpenStatusPage.Shared.DataTransferObjects.Incidents;
using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Client.Pages.Dashboard.Incidents
{
    public partial class AddIncidentDialog
    {
        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        public string Name { get; set; }

        public IncidentSeverity Severity { get; set; }

        private void AddIncidentAsync()
        {
            if (string.IsNullOrWhiteSpace(Name)) MudDialog.Close(DialogResult.Cancel());

            MudDialog.Close(DialogResult.Ok(new IncidentMetaDto
            {
                Name = Name,
                LatestStatus = IncidentStatus.Created,
                LatestSeverity = Severity
            }));
        }
    }
}
