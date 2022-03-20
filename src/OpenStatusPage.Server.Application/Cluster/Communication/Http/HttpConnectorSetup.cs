using Microsoft.AspNetCore.Routing;

namespace OpenStatusPage.Server.Application.Cluster.Communication.Http
{
    public static class HttpConnectorSetup
    {
        public static IEndpointRouteBuilder UseHttpConnector(this IEndpointRouteBuilder endpoints)
             => HttpConnector.ConfigureEndpointRouteBuilder(endpoints);
    }
}
