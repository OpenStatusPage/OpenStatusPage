using OpenStatusPage.Server.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenStatusPage.Server.Domain.Entities
{
    public abstract class EntityBase : IHasVersion
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; }


        [ConcurrencyCheck]
        public long Version { get; set; }
    }
}
