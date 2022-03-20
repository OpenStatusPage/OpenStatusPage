using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OpenStatusPage.Client.Application.Authentication;

namespace OpenStatusPage.Client.Pages.Dashboard
{
    public partial class Logout
    {
        [Inject]
        protected CredentialService CredentialService { get; set; }

        [Inject]
        protected AuthenticationStateProvider AuthStateProvider { get; set; }

        [Inject]
        protected NavigationManager Navigation { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await CredentialService.SetDashboardCredentialsAsync(null!);

            await (AuthStateProvider as GlobalAuthenticationStateProvider).RemoveRoleAsync(GlobalAuthenticationStateProvider.DASHBOARD_ACCESS_ROLE);

            Navigation.NavigateTo($"/dashboard");

            await base.OnInitializedAsync();
        }
    }
}
