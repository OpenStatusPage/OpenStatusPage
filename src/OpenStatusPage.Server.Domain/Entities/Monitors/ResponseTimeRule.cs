using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors;

public class ResponseTimeRule : MonitorRule
{
    [Required]
    public ushort ComparisonValue { get; set; }

    [Required]
    public NumericComparisonType ComparisonType { get; set; }
}
