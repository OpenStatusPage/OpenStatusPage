using OpenStatusPage.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenStatusPage.Server.Domain.Entities.Cluster
{
    public class ClusterMember
    {
        [NotMapped]
        public string Id { get; set; }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Uri Endpoint { get; set; }

        [NotMapped]
        public List<string> Tags { get; set; }

        [NotMapped]
        public ClusterMemberAvailability Availability { get; set; }

        [NotMapped]
        public double? AvgCpuLoad { get; set; }

        [NotMapped]
        public bool IsLeader { get; set; }

        [NotMapped]
        public bool IsLocal { get; set; }
    }
}
