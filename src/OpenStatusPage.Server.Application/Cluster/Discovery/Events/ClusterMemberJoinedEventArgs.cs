using OpenStatusPage.Server.Domain.Entities.Cluster;

namespace OpenStatusPage.Server.Application.Cluster.Discovery.Events;

public class ClusterMemberJoinedEventArgs
{
    public ClusterMember Member { get; set; }

    public ClusterMemberJoinedEventArgs(ClusterMember member)
    {
        Member = member;
    }
}
