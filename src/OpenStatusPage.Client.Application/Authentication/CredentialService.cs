using Blazored.LocalStorage;
using OpenStatusPage.Shared.Models.Credentials;

namespace OpenStatusPage.Client.Application.Authentication;

public class CredentialService
{
    private readonly ILocalStorageService _localStorage;

    protected DashboardCredentials DashboardCredentials { get; set; }

    protected List<StatusPageCredentials> StatusPageCredentials { get; set; }

    public CredentialService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected async Task ReloadDashboardCredentialsFromLocalStorageAsync(CancellationToken cancellationToken = default)
    {
        DashboardCredentials = await _localStorage.GetItemAsync<DashboardCredentials>("DashboardCredentials", cancellationToken);
    }

    protected async Task ReloadStatusPageCredentialsFromLocalStorageAsync(CancellationToken cancellationToken = default)
    {
        StatusPageCredentials = await _localStorage.GetItemAsync<List<StatusPageCredentials>>("StatusPageCredentials", cancellationToken) ?? new();
    }

    public async Task ReloadFromLocalStorageAsync(CancellationToken cancellationToken = default)
    {
        await ReloadDashboardCredentialsFromLocalStorageAsync(cancellationToken);

        await ReloadStatusPageCredentialsFromLocalStorageAsync(cancellationToken);
    }

    public async Task<DashboardCredentials> GetDashboardCredentialsAsync(CancellationToken cancellationToken = default)
    {
        if (DashboardCredentials == null) await ReloadDashboardCredentialsFromLocalStorageAsync(cancellationToken);

        return DashboardCredentials;
    }

    public async Task SetDashboardCredentialsAsync(DashboardCredentials credentials, CancellationToken cancellationToken = default)
    {
        if (credentials != null)
        {
            await _localStorage.SetItemAsync("DashboardCredentials", credentials, cancellationToken);
        }
        else
        {
            await _localStorage.RemoveItemAsync("DashboardCredentials", cancellationToken);
        }

        DashboardCredentials = credentials!;
    }

    public async Task<List<StatusPageCredentials>> GetStatusPageCredentialsAsync(CancellationToken cancellationToken = default)
    {
        if (StatusPageCredentials == null) await ReloadStatusPageCredentialsFromLocalStorageAsync(cancellationToken);

        return StatusPageCredentials;
    }

    public async Task SetStatusPageCredentialsAsync(List<StatusPageCredentials> credentials, CancellationToken cancellationToken = default)
    {
        if (credentials != null && credentials.Count > 0)
        {
            await _localStorage.SetItemAsync("StatusPageCredentials", credentials, cancellationToken);
        }
        else
        {
            await _localStorage.RemoveItemAsync("StatusPageCredentials", cancellationToken);
        }

        StatusPageCredentials = credentials!;
    }
}
