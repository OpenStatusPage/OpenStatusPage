using DotNext.Buffers;
using DotNext.IO;
using DotNext.Net;
using DotNext.Net.Cluster;
using DotNext.Net.Cluster.Consensus.Raft.Membership;
using DotNext.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenStatusPage.Server.Application.Cluster.Discovery.Commands;
using OpenStatusPage.Server.Application.Configuration;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Cluster.Consensus.Raft
{
    public class DatabaseClusterConfigurationStorage : InMemoryClusterConfigurationStorage<HttpEndPoint>, ISingletonService, IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly EnvironmentSettings _environmentSettings;

        public DatabaseClusterConfigurationStorage(IServiceProvider services, EnvironmentSettings environmentSettings)
        {
            _services = services;
            _environmentSettings = environmentSettings;
        }

        protected override void Encode(HttpEndPoint address, ref BufferWriterSlim<byte> output)
            => output.WriteEndPoint(address);

        protected override HttpEndPoint Decode(ref SequenceReader reader)
            => (HttpEndPoint)reader.ReadEndPoint();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var builder = CreateActiveConfigurationBuilder();

            //Add the distinct list of known endpoints from DB + env args
            var mediator = _services.GetRequiredService<ScopedMediatorExecutor>();

            var dbEndpoints = mediator.Send(new ClusterMembersQuery(), cancellationToken).GetAwaiter().GetResult();

            var clusterMembers = _environmentSettings.ConnectEndpoints //Connect endpoints from env settings
                .Concat(dbEndpoints?.ClusterMembers?.Select(x => x.Endpoint) ?? Array.Empty<Uri>()) //Known endpoints from db
                .Append(_environmentSettings.PublicEndpoint) //Ourself
                .Distinct();

            foreach (var endpoint in clusterMembers)
            {
                var httpEndPoint = new HttpEndPoint(endpoint);
                builder.Add(ClusterMemberId.FromEndPoint(httpEndPoint), httpEndPoint);
            }

            builder.Build();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
