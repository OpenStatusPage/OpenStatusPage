using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenStatusPage.Server.Domain.Entities.Notifications.History;

namespace OpenStatusPage.Server.Persistence.Configurations
{
    public class NotificationHistoryRecordConfiguration : IEntityTypeConfiguration<NotificationHistoryRecord>
    {
        public void Configure(EntityTypeBuilder<NotificationHistoryRecord> builder)
        {
            builder.HasKey(record => new { record.MonitorId, record.StatusUtc });
        }
    }
}
