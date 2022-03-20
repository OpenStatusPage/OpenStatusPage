using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenStatusPage.Server.Domain.Entities.Incidents;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.StatusPages;
using OpenStatusPage.Server.Domain.Interfaces;

namespace OpenStatusPage.Server.Persistence.Configurations;

public class IncidentTimelineItemConfiguration : IEntityTypeConfiguration<IncidentTimelineItem>
{
    public void Configure(EntityTypeBuilder<IncidentTimelineItem> builder)
    {
        builder.ToTable(IPolymorph.GetTypeAsMulitpleString<IncidentTimelineItem>());
    }
}

public class MonitorRuleConfiguration : IEntityTypeConfiguration<MonitorRule>
{
    public void Configure(EntityTypeBuilder<MonitorRule> builder)
    {
        builder.ToTable(IPolymorph.GetTypeAsMulitpleString<MonitorRule>());
    }
}

public class MonitorSummaryConfiguration : IEntityTypeConfiguration<MonitorSummary>
{
    public void Configure(EntityTypeBuilder<MonitorSummary> builder)
    {
        builder.ToTable(IPolymorph.GetTypeAsMulitpleString<MonitorSummary>());
    }
}

public class LabeledMonitorConfiguration : IEntityTypeConfiguration<LabeledMonitor>
{
    public void Configure(EntityTypeBuilder<LabeledMonitor> builder)
    {
        builder.ToTable(IPolymorph.GetTypeAsMulitpleString<LabeledMonitor>());
    }
}
