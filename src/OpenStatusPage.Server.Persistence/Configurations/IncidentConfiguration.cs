using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenStatusPage.Server.Domain.Entities.Incidents;

namespace OpenStatusPage.Server.Persistence.Configurations
{
    public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
    {
        public void Configure(EntityTypeBuilder<Incident> builder)
        {
            builder
                .HasMany(x => x.AffectedServices)
                .WithMany(x => x.InvolvedInIncidents)
                .UsingEntity<IncidentMonitorMapping>(x =>
                {
                    x.HasKey(x => new { x.MonitorBaseId, x.IncidentId });

                    x.ToTable($"{nameof(IncidentMonitorMapping)}s");
                });
        }

        public class IncidentMonitorMapping
        {
            public string IncidentId { get; set; }

            public string MonitorBaseId { get; set; }
        }
    }
}
