using OpenStatusPage.Server.Domain.Entities.Cluster;
using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Server.Application.Cluster.Discovery.Events;

public class ClusterMemberStatusChangedEventArgs
{
    public ClusterMember Member { get; set; }

    public ClusterMemberAvailability OldAvailability { get; set; }

    public ClusterMemberAvailability NewAvailability { get; set; }

    public ClusterMemberStatusChangedEventArgs(ClusterMember member, ClusterMemberAvailability oldAvailability, ClusterMemberAvailability newAvailability)
    {
        Member = member;
        OldAvailability = oldAvailability;
        NewAvailability = newAvailability;
    }
}
