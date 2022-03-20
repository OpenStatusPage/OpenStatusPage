using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenStatusPage.Server.Domain.Entities.Cluster
{
    public class RaftLogMetaEntry
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Index { get; set; }

        public long Term { get; set; }
    }
}
