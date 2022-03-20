using OpenStatusPage.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors.Http;

public class ResponseHeaderRule : MonitorRule
{
    [Required]
    public string Key { get; set; }

    public string? ComparisonValue { get; set; }

    public StringComparisonType ComparisonType { get; set; }
}
