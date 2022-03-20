using Microsoft.AspNetCore.Authentication;

namespace OpenStatusPage.Server.Application.Authentication
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string HEADER_NAME = "X-Api-Key";
        public const string SCHEME = "ApiKeyAuthenticationScheme";
    }
}
