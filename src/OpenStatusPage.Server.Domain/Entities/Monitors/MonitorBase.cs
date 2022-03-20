using OpenStatusPage.Server.Domain.Attributes;
using OpenStatusPage.Server.Domain.Entities.Incidents;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Domain.Entities.Monitors
{
    [PolymorphBaseType]
    public class MonitorBase : EntityBase, IPolymorph
    {
        [Required]
        public bool Enabled { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public TimeSpan Interval { get; set; }

        public ushort? Retries { get; set; }

        public TimeSpan? RetryInterval { get; set; }

        public TimeSpan? Timeout { get; set; }

        public int WorkerCount { get; set; }

        public string Tags { get; set; }

        public virtual ICollection<MonitorRule> Rules { get; set; }

        public virtual ICollection<Incident> InvolvedInIncidents { get; set; }

        public virtual ICollection<NotificationProvider> NotificationProviders { get; set; }
    }
}
