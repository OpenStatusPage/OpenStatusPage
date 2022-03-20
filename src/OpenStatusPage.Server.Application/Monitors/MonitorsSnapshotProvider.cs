using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Cluster.Consensus;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;

namespace OpenStatusPage.Server.Application.Monitors
{
    public class MonitorsSnapshotProvider : ISnapshotDataProvider
    {
        private readonly IMediator _mediator;

        public MonitorsSnapshotProvider(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<List<MessageBase>> GetDataAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<MessageBase>();

            var monitors = (await _mediator.Send(new MonitorsQuery
            {
                Query = new(query => query
                    .Include(x => x.Rules)
                    .Include(x => x.NotificationProviders))
            }, cancellationToken))?.Monitors;

            if (monitors != null)
            {
                foreach (var monitor in monitors)
                {
                    //Convert into plain objects using ids only
                    monitor.NotificationProviders = monitor.NotificationProviders
                        .Select(x => new NotificationProvider() { Id = x.Id })
                        .ToList();

                    result.Add(new CreateOrUpdateMonitorCmd()
                    {
                        Data = monitor
                    });
                }
            }

            return result;
        }

        [SnapshotApplyDataOrder(10)]
        public async Task ApplyDataAsync(List<MessageBase> data, CancellationToken cancellationToken = default)
        {
            var monitors = (await _mediator.Send(new MonitorsQuery(), cancellationToken))?.Monitors;

            if (monitors != null)
            {
                foreach (var monitor in monitors)
                {
                    //Local entity does not existing in the snapshot data from the leader anymore, remove it
                    if (!data.Any(x => x is CreateOrUpdateMonitorCmd createOrUpdate && createOrUpdate.Data.Id == monitor.Id))
                    {
                        await _mediator.Send(new DeleteMonitorCmd()
                        {
                            MonitorId = monitor.Id
                        }, cancellationToken);
                    }
                }
            }

            foreach (var message in data)
            {
                switch (message)
                {
                    case CreateOrUpdateMonitorCmd createOrUpdate:
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
