using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenStatusPage.Server.Domain.Entities.StatusHistory;

namespace OpenStatusPage.Server.Persistence.Configurations
{
    public class StatusHistoryRecordConfiguration : IEntityTypeConfiguration<StatusHistoryRecord>
    {
        public void Configure(EntityTypeBuilder<StatusHistoryRecord> builder)
        {
            builder.HasKey(record => new { record.MonitorId, record.FromUtc });
        }
    }
}
