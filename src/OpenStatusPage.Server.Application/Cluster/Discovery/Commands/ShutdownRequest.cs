using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Cluster.Discovery.Commands
{
    public class ShutdownRequest : MessageBase
    {
        public class Handler : IRequestHandler<ShutdownRequest>
        {
            private readonly ClusterService _clusterService;

            public Handler(ClusterService clusterService)
            {
                _clusterService = clusterService;
            }

            public async Task<Unit> Handle(ShutdownRequest request, CancellationToken cancellationToken)
            {
                _ = Task.Run(async () =>
                {
                    //Give the chance to respond to the shutdown request
                    await Task.Delay(5000);

                    //Request the application exit
                    await _clusterService.RequestShutdownAsync(cancellationToken);
                }, cancellationToken);

                return Unit.Value;
            }
        }
    }
}
