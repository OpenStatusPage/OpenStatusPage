using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Cluster.Consensus;
using OpenStatusPage.Server.Application.Incidents.Commands;
using OpenStatusPage.Server.Domain.Entities.Monitors;

namespace OpenStatusPage.Server.Application.Incidents
{
    public class IncidentsSnapshotProvider : ISnapshotDataProvider
    {
        private readonly IMediator _mediator;

        public IncidentsSnapshotProvider(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<List<MessageBase>> GetDataAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<MessageBase>();

            var incidents = (await _mediator.Send(new IncidentsQuery()
            {
                Query = new(query => query
                    .Include(x => x.Timeline)
                    .Include(x => x.AffectedServices))
            }, cancellationToken))?.Incidents;

            if (incidents != null)
            {
                foreach (var incident in incidents)
                {
                    //Convert into plain objects using ids only
                    incident.AffectedServices = incident.AffectedServices
                        .Select(x => new MonitorBase() { Id = x.Id })
                        .ToList();

                    result.Add(new CreateOrUpdateIncidentCmd()
                    {
                        Data = incident
                    });
                }
            }

            return result;
        }

        [SnapshotApplyDataOrder(20)]
        public async Task ApplyDataAsync(List<MessageBase> data, CancellationToken cancellationToken = default)
        {
            var incidents = (await _mediator.Send(new IncidentsQuery(), cancellationToken))?.Incidents;

            if (incidents != null)
            {
                foreach (var incident in incidents)
                {
                    //Local entity does not existing in the snapshot data from the leader anymore, remove it
                    if (!data.Any(x => x is CreateOrUpdateIncidentCmd createOrUpdate && createOrUpdate.Data.Id == incident.Id))
                    {
                        await _mediator.Send(new DeleteIncidentCmd()
                        {
                            IncidentId = incident.Id
                        }, cancellationToken);
                    }
                }
            }

            foreach (var message in data)
            {
                switch (message)
                {
                    case CreateOrUpdateIncidentCmd createOrUpdate:
                    {
                        await _mediator.Send(createOrUpdate, cancellationToken);
                        break;
                    }

                    default: throw new NotImplementedException();
                }
            }
        }
    }
}
