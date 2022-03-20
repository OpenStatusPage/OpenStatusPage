using OpenStatusPage.Server.Domain.Entities.StatusPages;
using System;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Configuration;

public class ApplicationSettings : EntityBase
{
    [Required]
    public TimeSpan StatusFlushInterval { get; set; }

    [Required]
    public ushort DaysMonitorHistory { get; set; }

    [Required]
    public ushort DaysIncidentHistory { get; set; }

    [Required]
    public string DefaultStatusPageId { get; set; }

    public virtual StatusPage DefaultStatusPage { get; set; }
}
