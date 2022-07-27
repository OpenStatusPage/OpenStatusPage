using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace OpenStatusPage.Client.Pages._Components
{
    public partial class AsyncButton : MudButton
    {
        /// <summary>
        /// Test shown on loading spinner
        /// </summary>
        [Parameter]
        public string RunningText { get; set; }

        /// <summary>
        /// Async event to show with progress spinner on button
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> AsyncAction { get; set; }

        public bool ActionRunning { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            OnClick = AsyncAction;

            await base.OnParametersSetAsync();
        }

        protected async Task OnClickHandlerAsync(MouseEventArgs ev)
        {
            if (Disabled || ActionRunning) return;

            ActionRunning = true;

            await InvokeAsync(StateHasChanged);

            await OnClickHandler(ev);

            ActionRunning = false;

            await InvokeAsync(StateHasChanged);
        }
    }
}