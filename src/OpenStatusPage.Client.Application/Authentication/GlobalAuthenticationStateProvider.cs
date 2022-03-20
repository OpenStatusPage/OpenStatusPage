using Microsoft.AspNetCore.Components.Authorization;
using OpenStatusPage.Shared.Requests.Credentials;
using System.Security.Claims;

namespace OpenStatusPage.Client.Application.Authentication;

public class GlobalAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly CredentialService _credentialService;
    private readonly TransparentHttpClient _httpClient;

    protected AuthenticationState AuthenticationState { get; set; }

    public const string UNKNOWN_CLAIMS_NAME = "UnknownClaims";

    public const string DASHBOARD_ACCESS_ROLE = "DashboardAccess";

    public GlobalAuthenticationStateProvider(CredentialService credentialService, TransparentHttpClient httpClient)
    {
        _credentialService = credentialService;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        lock (this)
        {
            if (AuthenticationState == null)
            {
                var placeholderIdentity = new ClaimsIdentity();
                placeholderIdentity.AddClaim(new Claim(UNKNOWN_CLAIMS_NAME, "true"));

                AuthenticationState = new AuthenticationState(new ClaimsPrincipal(placeholderIdentity));

                _ = Task.Run(() => ValidateCredentialsAsync());
            }
        }

        return AuthenticationState;
    }

    public async Task ValidateCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var authentication = AuthenticationState;

        if (AuthenticationState.User.Claims.Any(x => x.Type == UNKNOWN_CLAIMS_NAME))
        {
            authentication = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity("OpenStatusPage")));
        }

        //See if we got valid credentials for the dashboard and or private status pages

        if (authentication?.User?.Identity is not ClaimsIdentity claimsIdentity) return;

        var statusPageCredentials = await _credentialService.GetStatusPageCredentialsAsync(cancellationToken) ?? new();

        var response = await _httpClient.SendAsync<CredentialsValidationRequest, CredentialsValidationRequest.Response>(HttpMethod.Post, "auth/v1/CredentialsValidation", new()
        {
            DashboardCredentials = await _credentialService.GetDashboardCredentialsAsync(cancellationToken),
            StatusPageCredentials = statusPageCredentials,
        }, default!, false, false, cancellationToken);

        if (response != null)
        {
            //Purge dashboard access
            await RemoveRoleAsync(claimsIdentity, DASHBOARD_ACCESS_ROLE, cancellationToken);

            //Add it back if we still have the rights
            if (response.ValidDashboardCredentials != null)
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, DASHBOARD_ACCESS_ROLE));

            }

            //Purge status page access
            foreach (var remove in statusPageCredentials)
            {
                await RemoveRoleAsync(claimsIdentity, remove.StatusPageId, cancellationToken);
            }

            //Add those back that we have access to
            foreach (var credential in response.ValidStatusPageCredentials)
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, credential.StatusPageId));
            }
        }

        //Update auth state
        AuthenticationState = authentication;

        NotifyAuthenticationStateChanged(Task.FromResult(AuthenticationState!));
    }

    public static async Task RemoveRoleAsync(ClaimsIdentity claimsIdentity, string role, CancellationToken cancellationToken = default)
    {
        var roleClaim = claimsIdentity.FindFirst(x => x.Type == ClaimTypes.Role && x.Value == role);

        if (roleClaim != null) claimsIdentity.RemoveClaim(roleClaim);
    }

    public async Task RemoveRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        if (AuthenticationState?.User?.Identity is not ClaimsIdentity claimsIdentity) return;

        await RemoveRoleAsync(claimsIdentity, role, cancellationToken);

        NotifyAuthenticationStateChanged(Task.FromResult(AuthenticationState!));
    }
}
