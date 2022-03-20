using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenStatusPage.Server.Domain.Entities.Monitors;

namespace OpenStatusPage.Server.Persistence.Configurations
{
    public class MonitorConfiguration : IEntityTypeConfiguration<MonitorBase>
    {
        public void Configure(EntityTypeBuilder<MonitorBase> builder)
        {
            builder
                .HasMany(x => x.NotificationProviders)
                .WithMany(x => x.UsedByMonitors)
                .UsingEntity<MonitorNotificationMapping>(x =>
                {
                    x.HasKey(x => new { x.MonitorBaseId, x.NotificationProviderId });

                    x.ToTable($"{nameof(MonitorNotificationMapping)}s");
                });
        }

        public class MonitorNotificationMapping
        {
            public string MonitorBaseId { get; set; }

            public string NotificationProviderId { get; set; }
        }
    }
}
