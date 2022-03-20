using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors.Http;

public class ResponseBodyRule : MonitorRule
{
    [Required]
    public string ComparisonValue { get; set; }

    [Required]
    public StringComparisonType ComparisonType { get; set; }
}
