using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using OpenStatusPage.Client;
using OpenStatusPage.Client.Application;
using OpenStatusPage.Client.Application.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//Culture
builder.Services.AddLocalization();

//Theme
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = true;
});

//Auto map properties between transfer and view model objects
builder.Services.AddAutoMapper(typeof(Program).Assembly);

//Clientside auth handling
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, GlobalAuthenticationStateProvider>();

//Local storage access
builder.Services.AddBlazoredLocalStorage();

//Business logic
builder.Services.AddScoped<HostEnvironmentParameters>(_ => new() { BaseAddress = builder.HostEnvironment.BaseAddress, Environment = builder.HostEnvironment.Environment });
builder.Services.AddScoped<TransparentHttpClient>();
builder.Services.AddScoped<ClusterEndpointsService>();
builder.Services.AddScoped<CredentialService>();

await builder.Build().RunAsync();
