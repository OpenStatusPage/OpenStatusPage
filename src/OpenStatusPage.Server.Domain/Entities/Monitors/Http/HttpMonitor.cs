using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors.Http;

public class HttpMonitor : MonitorBase
{
    [Required]
    public string Url { get; set; }

    [Required]
    public HttpVerb Method { get; set; }

    [Required]
    public ushort MaxRedirects { get; set; }

    public string? Headers { get; set; }

    public string? Body { get; set; }

    public HttpAuthenticationScheme AuthenticationScheme { get; set; }

    /// <summary>
    /// Stores either the username or the bearer token
    /// </summary>
    public string? AuthenticationBase { get; set; }

    /// <summary>
    /// Stores the correspoding password to <see cref="AuthenticationBase"/>
    /// </summary>
    public string? AuthenticationAdditional { get; set; }
}
