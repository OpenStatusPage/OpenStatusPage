using DotNext.Net.Cluster.Consensus.Raft;

namespace OpenStatusPage.Server.Application.Cluster.Consensus.Raft
{
    public class ClusterMemberLifetime : IClusterMemberLifetime
    {
        public event EventHandler<(IRaftCluster cluster, IDictionary<string, string> metadata)> OnStart;
        public event EventHandler<IRaftCluster> OnStop;

        void IClusterMemberLifetime.OnStart(IRaftCluster cluster, IDictionary<string, string> metadata)
        {
            OnStart?.Invoke(this, (cluster, metadata));
        }

        void IClusterMemberLifetime.OnStop(IRaftCluster cluster)
        {
            OnStop?.Invoke(this, cluster);
        }
    }
}
