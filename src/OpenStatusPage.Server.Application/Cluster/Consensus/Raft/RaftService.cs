using DotNext.IO.Log;
using DotNext.Net.Cluster;
using DotNext.Net.Cluster.Consensus.Raft;
using DotNext.Net.Cluster.Consensus.Raft.Http;
using DotNext.Net.Cluster.Consensus.Raft.Membership;
using DotNext.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Cluster.Communication.Http;
using OpenStatusPage.Server.Application.Cluster.Consensus.Raft.LogEntries;
using OpenStatusPage.Server.Application.Cluster.Consensus.Raft.States;
using OpenStatusPage.Server.Application.Configuration;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Cluster.Consensus.Raft
{
    public class RaftService : IConsensusService, ISingletonService
    {
        private readonly IRaftHttpCluster _raftCluster;
        private readonly EnvironmentSettings _environmentSettings;
        private readonly ClusterMemberLifetime _clusterMemberLifetime;

        public event EventHandler OnStart;

        public event EventHandler OnStop;

        public event EventHandler OnReplicationCompleted;

        public event EventHandler<MessageBase> OnReplicatedMessage;

        public event EventHandler OnMessageQueueCleared;

        public event EventHandler<IClusterMember> OnMemberJoined;

        public event EventHandler<IClusterMember> OnMemberLeft;

        public event EventHandler<IClusterMember> OnLeaderChanged;

        public bool IsLeader { get; set; }

        public RaftService(IRaftHttpCluster raftCluster,
                           IClusterMemberLifetime clusterMemberLifetime,
                           EnvironmentSettings environmentSettings)
        {
            _raftCluster = raftCluster;
            _environmentSettings = environmentSettings;
            _clusterMemberLifetime = (ClusterMemberLifetime)clusterMemberLifetime;

            _clusterMemberLifetime.OnStart += (sender, data) => OnClusterStart(data.cluster, data.metadata);
            _clusterMemberLifetime.OnStop += (sender, data) => OnClusterStop(data);
        }

        public static IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            return services
                .AddSingleton<IClusterConfigurationStorage<HttpEndPoint>>(x => x.GetService<DatabaseClusterConfigurationStorage>())
                .AddSingleton<PersistentMessageReplicatorState>()
                .AddSingleton<IPersistentState>(x => x.GetService<PersistentMessageReplicatorState>())
                .AddSingleton<IAuditTrail<IRaftLogEntry>>(x => x.GetService<PersistentMessageReplicatorState>())
                .AddSingleton<IClusterMemberLifetime, ClusterMemberLifetime>()
                .AddSingleton<IHttpMessageHandlerFactory, RaftClientHandlerFactory>();
        }

        public static IHostBuilder ConfigureHostBuilder(IHostBuilder builder)
        {
            builder.JoinCluster((options, configuration, host) =>
            {
                var environmentSettings = EnvironmentSettings.Create(configuration);

                options.PublicEndPoint = new HttpEndPoint(environmentSettings.PublicEndpoint);
                options.ColdStart = false;

                options.ProtocolPath = $"{environmentSettings.PublicEndpoint.LocalPath}cluster-message-bus/v1/consensus/raft";

                options.LowerElectionTimeout = environmentSettings.ConnectionTimeout;
                options.UpperElectionTimeout = options.LowerElectionTimeout * 2;
            });

            return builder;
        }

        public static IApplicationBuilder ConfigureApplicationBuilder(IApplicationBuilder app)
        {
            var environmentSettings = app.ApplicationServices.GetRequiredService<EnvironmentSettings>();

            return app
                .Use(async (context, next) =>
                {
                    //Hotfix for issue with dotNext raft not absuing content length incorrectly.
                    if (context.Request.Path.StartsWithSegments(new($"{environmentSettings.PublicEndpoint.LocalPath}cluster-message-bus/v1/consensus/raft")))
                    {
                        if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value != 0)
                        {
                            context.Request.ContentLength = null;
                        }
                    }

                    await next(context);
                })
                .UseConsensusProtocolHandler()
                .RedirectToLeader($"{environmentSettings.PublicEndpoint.LocalPath}{HttpConnector.MESSAGE_BUS_URL_LEADER}");
        }

        public void OnClusterStart(IRaftCluster cluster, IDictionary<string, string> metadata)
        {
            //Add meta data
            metadata.Add("memberid", _environmentSettings.Id);
            metadata.Add("membertags", string.Join(';', _environmentSettings.Tags));

            //Subscribe cluster events
            cluster.PeerDiscovered += (peerMesh, peer) =>
            {
                OnMemberJoined?.Invoke(cluster, ((ClusterMemberEventArgs)peer).Member);
            };

            cluster.PeerGone += (peerMesh, peer) =>
            {
                OnMemberLeft?.Invoke(cluster, ((ClusterMemberEventArgs)peer).Member);
            };

            cluster.LeaderChanged += (icluster, clustermember) =>
            {
                IsLeader = clustermember != null && !clustermember.IsRemote;

                (cluster.AuditTrail as RaftStateBase).IsLeader = IsLeader;

                OnLeaderChanged?.Invoke(icluster, clustermember!);
            };

            (cluster.AuditTrail as PersistentMessageReplicatorState).OnMessageReceived += (auditTrail, message) =>
            {
                if (message is ReplicatedMessage replicatedMessage)
                {
                    OnReplicatedMessage?.Invoke(this, replicatedMessage.Message);
                }
            };

            (cluster.AuditTrail as PersistentMessageReplicatorState).OnMessageQueueCleared += (auditTrail, args) =>
            {
                OnMessageQueueCleared?.Invoke(this, args);
            };

            cluster.ReplicationCompleted += (cluster, member) =>
            {
                var auditTrail = cluster.AuditTrail as PersistentMessageReplicatorState;

                if (auditTrail.LastCommittedEntryIndex >= auditTrail.LastUncommittedEntryIndex)
                {
                    OnReplicationCompleted?.Invoke(cluster, default!);
                }

                if (!auditTrail.HasBufferedMessages())
                {
                    OnMessageQueueCleared?.Invoke(this, null!);
                }
            };

            OnStart?.Invoke(this, default!);
        }

        public void OnClusterStop(IRaftCluster cluster)
        {
            OnStop?.Invoke(this, default!);
        }

        public Task<bool> AddMemberAsync(Uri endpoint, CancellationToken cancellationToken = default)
        {
            var httpEndpoint = new HttpEndPoint(endpoint);
            return _raftCluster.AddMemberAsync(ClusterMemberId.FromEndPoint(httpEndpoint), httpEndpoint, cancellationToken);
        }

        public Task<bool> RemoveMemberAsync(Uri endpoint, CancellationToken cancellationToken = default)
        {
            return _raftCluster.RemoveMemberAsync(new HttpEndPoint(endpoint), cancellationToken);
        }

        public Task<IReadOnlyCollection<IRaftClusterMember>> GetMembersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(((IRaftCluster)_raftCluster).Members);
        }

        public Task<IRaftClusterMember> GetMemberByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(((IRaftCluster)_raftCluster).Members.FirstOrDefault(x => x.EndPoint.ToString().StartsWith(endpoint))!);
        }

        public async Task<bool> ReplicateAsync(MessageBase message, CancellationToken cancellationToken = default)
        {
            var entry = new ReplicatedMessage(message)
            {
                Term = _raftCluster.Term,
                Timestamp = DateTimeOffset.UtcNow
            };

            return await _raftCluster.ReplicateAsync(entry, cancellationToken);
        }
    }
}
