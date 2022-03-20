using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Authentication;
using OpenStatusPage.Server.Application.Configuration;
using OpenStatusPage.Server.Domain.Entities.Cluster;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OpenStatusPage.Server.Application.Cluster.Communication.Http
{
    public class HttpConnector : INetworkConnector
    {
        public const string MESSAGE_BUS_URL = "cluster-message-bus/v1/networkconnector";
        public const string MESSAGE_BUS_URL_LEADER = $"{MESSAGE_BUS_URL}/leader";
        private readonly EnvironmentSettings _environmentSettings;

        public HttpConnector(EnvironmentSettings environmentSettings)
        {
            _environmentSettings = environmentSettings;
        }

        public static IEndpointRouteBuilder ConfigureEndpointRouteBuilder(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost($"/{MESSAGE_BUS_URL}", async context => await (context.RequestServices.GetService<INetworkConnector>() as HttpConnector).HandleRequestAsync(context));
            endpoints.MapPost($"/{MESSAGE_BUS_URL_LEADER}", async context => await (context.RequestServices.GetService<INetworkConnector>() as HttpConnector).HandleRequestAsync(context));

            return endpoints;
        }

        public async Task<TResponse> SendAsync<TResponse>(ClusterMember member, RequestBase<TResponse> request, bool redirectToLeader = false, CancellationToken cancellationToken = default)
        {
            var client = new HttpClient();

            try
            {
                var url = $"{member.Endpoint}{(redirectToLeader ? MESSAGE_BUS_URL_LEADER : MESSAGE_BUS_URL)}";

                client.DefaultRequestHeaders.Add(ApiKeyAuthenticationOptions.HEADER_NAME, _environmentSettings.ApiKey);

                var response = await client.PostAsJsonAsync(url, WrapWithType(request), cancellationToken);

                if (!response.IsSuccessStatusCode) throw new Exception("Received failure status code.");

                // Only write a body when it's not an empty response (from MessageBase represented by Unit)
                if (!typeof(TResponse).IsAssignableTo(typeof(Unit)) && response.Content.Headers.ContentLength > 0)
                {
                    return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
                }

                return default;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not deliver request.", ex);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Future proof member access of connector")]
        public async Task HandleRequestAsync(HttpContext context)
        {
            try
            {
                var mediator = context.RequestServices.GetService<IMediator>();

                var wrapper = await context.Request.ReadFromJsonAsync<JsonMessageWrapper>();

                if (wrapper == null) throw new Exception("Unable to parse request data.");

                var requestObject = UnwrapFromType(wrapper);

                var result = await mediator.Send(requestObject);

                context.Response.StatusCode = (int)HttpStatusCode.OK;

                // Only write a body when it's not an empty response (from MessageBase represented by Unit)
                if (result != null && !result.GetType().IsAssignableTo(typeof(Unit)))
                {
                    await context.Response.WriteAsJsonAsync(result);
                }

                return;
            }
            catch (Exception ex)
            {
            }

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }

        protected static JsonMessageWrapper WrapWithType(object request)
        {
            return new JsonMessageWrapper()
            {
                Type = $"{request.GetType().FullName}", //todo do not send full name but build local lookup index table that will be the same on all clients?
                Value = JsonSerializer.Serialize(request)
            };
        }

        protected static object UnwrapFromType(JsonMessageWrapper wrapper)
        {
            var type = Type.GetType(wrapper.Type);

            if (type == null) throw new Exception("Received unknown type.");

            if (!type.IsAssignableTo(typeof(IRequestBase))) throw new Exception("Received incompatible type.");

            return JsonSerializer.Deserialize(wrapper.Value, type);
        }
    }
}
