using OpenStatusPage.Server.Domain.Attributes;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Interfaces;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Notifications.Providers;

[PolymorphBaseType]
public class NotificationProvider : EntityBase, IPolymorph
{
    [Required]
    public string Name { get; set; }

    [Required]
    public bool Enabled { get; set; }

    [Required]
    public bool DefaultForNewMonitors { get; set; }

    public virtual ICollection<MonitorBase> UsedByMonitors { get; set; }
}
