using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using OpenStatusPage.Client.Application.Authentication;
using OpenStatusPage.Shared.Models.Credentials;
using System.ComponentModel.DataAnnotations;
using static OpenStatusPage.Client.Application.TransparentHttpClient;

namespace OpenStatusPage.Client.Pages.Dashboard
{
    public partial class DashboardBase : IAsyncDisposable
    {
        [Inject]
        protected IBreakpointService BreakpointListener { get; set; }

        [Inject]
        protected AuthenticationStateProvider AuthStateProvider { get; set; }

        [Inject]
        protected IWebAssemblyHostEnvironment HostEnvironment { get; set; }

        [Inject]
        protected CredentialService CredentialService { get; set; }

        public LoginModel LoginViewModel { get; set; }

        protected Guid _subscriptionId;
        protected bool _drawerOpen = false;

        protected bool _showSpinner = true;

        protected string AccessRoles { get; set; }

        protected HeaderEntry DashboardApiKeyHeader { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var credentials = await CredentialService.GetDashboardCredentialsAsync();

            LoginViewModel = new()
            {
                Endpoint = credentials?.ConnectEndpoint ?? HostEnvironment.BaseAddress,
                ApiKey = credentials?.ApiKey ?? ""
            };

            DashboardApiKeyHeader = new("X-Api-Key", LoginViewModel.ApiKey);

            //Authentication
            AccessRoles = GlobalAuthenticationStateProvider.DASHBOARD_ACCESS_ROLE;
            AuthStateProvider.AuthenticationStateChanged += AuthStateProvider_AuthenticationStateChangedAsync;
            _ = Task.Run(() => AuthStateProvider_AuthenticationStateChangedAsync(AuthStateProvider.GetAuthenticationStateAsync()));

            //Handling for sidebar on resize
            _subscriptionId = (await BreakpointListener.Subscribe((breakpoint) =>
            {
                if (breakpoint > Breakpoint.Md)
                {
                    _drawerOpen = false;
                    InvokeAsync(StateHasChanged);
                }
            }, new ResizeOptions { ReportRate = 100, NotifyOnBreakpointOnly = true })).SubscriptionId;

            await base.OnInitializedAsync();
        }

        protected async void AuthStateProvider_AuthenticationStateChangedAsync(Task<AuthenticationState> task)
        {
            if (!(await task).User.Claims.Any(x => x.Type == GlobalAuthenticationStateProvider.UNKNOWN_CLAIMS_NAME))
            {
                _showSpinner = false;
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task SubmitLoginFormAsync()
        {
            await CredentialService.SetDashboardCredentialsAsync(new DashboardCredentials
            {
                ConnectEndpoint = LoginViewModel.Endpoint,
                ApiKey = LoginViewModel.ApiKey
            });

            DashboardApiKeyHeader = new("X-Api-Key", LoginViewModel.ApiKey);

            await (AuthStateProvider as GlobalAuthenticationStateProvider).ValidateCredentialsAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await BreakpointListener.Unsubscribe(_subscriptionId);

            AuthStateProvider.AuthenticationStateChanged -= AuthStateProvider_AuthenticationStateChangedAsync;
        }

        protected void DrawerToggle()
        {
            _drawerOpen = !_drawerOpen;
        }

        public class LoginModel
        {
            [Required]
            [Url(ErrorMessage = "Invalid url. Correct format: https://osp.example.org")]
            public string Endpoint { get; set; }

            [Required]
            [StringLength(36, ErrorMessage = "API key format is invalid. Correct format: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX", MinimumLength = 36)]
            public string ApiKey { get; set; }
        }
    }
}
