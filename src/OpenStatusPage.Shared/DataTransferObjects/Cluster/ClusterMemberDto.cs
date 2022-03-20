using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Cluster
{
    public class ClusterMemberDto
    {
        public string Id { get; set; }

        public Uri Endpoint { get; set; }

        public List<string> Tags { get; set; }

        public ClusterMemberAvailability Availability { get; set; }

        public double? AvgCpuLoad { get; set; }

        public bool IsLeader { get; set; }
    }
}
