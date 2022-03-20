using MudBlazor;
using OpenStatusPage.Client.Pages._Components;

namespace OpenStatusPage.Client.Extensions
{
    public static class DialogServiceExtensions
    {
        public static async Task<bool> ConfirmAsync(
            this IDialogService dialogService,
            string title,
            string text,
            string confirmText = default!,
            string cancelText = default!,
            Color submitColor = Color.Primary,
            string confirmIcon = default!,
            string cancelIcon = default!)
        {
            return !(await dialogService.Show<ConfirmationDialog>(title, new DialogParameters
            {
                { "Text", text },
                { "ConfirmText", confirmText },
                { "CancelText", cancelText },
                { "Color", submitColor },
                { "ConfirmIcon", confirmIcon},
                { "CancelIcon", cancelIcon}
            }).Result).Cancelled;
        }
    }
}
