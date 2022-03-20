using Microsoft.AspNetCore.Components;

namespace OpenStatusPage.Client.Pages._Components
{
    public partial class SelfRefresh
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Navigation.NavigateTo(Navigation.Uri, true);

            await base.OnInitializedAsync();
        }
    }
}
