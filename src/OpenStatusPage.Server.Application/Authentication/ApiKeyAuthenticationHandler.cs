using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenStatusPage.Server.Application.Configuration;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace OpenStatusPage.Server.Application.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private readonly EnvironmentSettings _environmentSettings;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            EnvironmentSettings environmentSettings) : base(options, logger, encoder, clock)
        {
            _environmentSettings = environmentSettings;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(ApiKeyAuthenticationOptions.HEADER_NAME))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!string.IsNullOrWhiteSpace(_environmentSettings.ApiKey) &&
                (Request.Headers[ApiKeyAuthenticationOptions.HEADER_NAME].First() != _environmentSettings.ApiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid api key."));
            }

            var claimsIdentity = new ClaimsIdentity(ApiKeyAuthenticationOptions.SCHEME);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), ApiKeyAuthenticationOptions.SCHEME);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
