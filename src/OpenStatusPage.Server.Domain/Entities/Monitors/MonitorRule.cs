using OpenStatusPage.Server.Domain.Attributes;
using OpenStatusPage.Server.Domain.Interfaces;
using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors
{
    [PolymorphBaseType]
    public class MonitorRule : EntityBase, IPolymorph
    {
        /// <summary>
        /// Index to order all rules 0 to N with 0 being evaluated first.
        /// </summary>
        [Required]
        public string MonitorId { get; set; }

        /// <summary>
        /// Index to order all rules 0 to N with 0 being evaluated first.
        /// </summary>
        [Required]
        public ushort OrderIndex { get; set; }

        /// <summary>
        /// If the rule is violated, what should the result status be. e.g. http status code 500 -> status unavailable
        /// </summary>
        [Required]
        public ServiceStatus ViolationStatus { get; set; }

        public virtual MonitorBase Monitor { get; set; }
    }
}
