using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors.Http;

public class StatusCodeRule : MonitorRule
{
    [Required]
    public ushort Value { get; set; }

    /// <summary>
    /// Optional upper value if the status code is a raenge and not a single value constraint
    /// </summary>
    public ushort? UpperRangeValue { get; set; }
}
