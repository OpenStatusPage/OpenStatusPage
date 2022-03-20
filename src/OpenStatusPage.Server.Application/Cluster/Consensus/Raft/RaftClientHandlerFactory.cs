using OpenStatusPage.Server.Application.Authentication;
using OpenStatusPage.Server.Application.Configuration;

namespace OpenStatusPage.Server.Application.Cluster.Consensus.Raft
{
    public class RaftClientHandlerFactory : IHttpMessageHandlerFactory
    {
        private readonly EnvironmentSettings _environmentSettings;

        public RaftClientHandlerFactory(EnvironmentSettings environmentSettings)
        {
            _environmentSettings = environmentSettings;
        }

        public HttpMessageHandler CreateHandler(string name)
        {
            if (name == "raftClient")
            {
                return new RaftMessageHandler(new SocketsHttpHandler { ConnectTimeout = TimeSpan.FromMilliseconds(_environmentSettings.ConnectionTimeout) }, _environmentSettings);
            }

            return new SocketsHttpHandler();
        }

        public class RaftMessageHandler : MessageProcessingHandler
        {
            private readonly EnvironmentSettings _environmentSettings;

            public RaftMessageHandler(HttpMessageHandler innerHandler, EnvironmentSettings environmentSettings) : base(innerHandler)
            {
                _environmentSettings = environmentSettings;
            }

            protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add(ApiKeyAuthenticationOptions.HEADER_NAME, _environmentSettings.ApiKey);

                return request;
            }

            protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
            {
                return response;
            }
        }
    }
}
