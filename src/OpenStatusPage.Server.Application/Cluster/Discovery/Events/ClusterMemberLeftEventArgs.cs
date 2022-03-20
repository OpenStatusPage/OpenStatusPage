using OpenStatusPage.Server.Domain.Entities.Cluster;

namespace OpenStatusPage.Server.Application.Cluster.Discovery.Events;

public class ClusterMemberLeftEventArgs
{
    public ClusterMember Member { get; set; }

    public ClusterMemberLeftEventArgs(ClusterMember member)
    {
        Member = member;
    }
}
