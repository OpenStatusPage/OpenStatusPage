using OpenStatusPage.Server.Domain.Entities.Cluster;

namespace OpenStatusPage.Server.Application.Cluster.Discovery.Events;

public class ClusterLeaderChangedEventArgs
{
    public ClusterMember? Leader { get; set; }

    public ClusterLeaderChangedEventArgs(ClusterMember? leader)
    {
        Leader = leader;
    }
}
