using Microsoft.AspNetCore.Components;

using MudBlazor;

namespace OpenStatusPage.Client.Pages._Components
{
    public partial class ConfirmationDialog
    {
        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        [Parameter]
        public string Text { get; set; }

        [Parameter]
        public string ConfirmText { get; set; }

        [Parameter]
        public string CancelText { get; set; }

        [Parameter]
        public Color Color { get; set; }

        [Parameter]
        public string ConfirmIcon { get; set; }

        [Parameter]
        public string CancelIcon { get; set; }

        void SubmitDialog() => MudDialog.Close(DialogResult.Ok(true));

        void CancelDialog() => MudDialog.Cancel();
    }
}
