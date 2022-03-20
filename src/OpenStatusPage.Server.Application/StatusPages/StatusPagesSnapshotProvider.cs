using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Cluster.Consensus;
using OpenStatusPage.Server.Application.StatusPages.Commands;

namespace OpenStatusPage.Server.Application.StatusPages
{
    public class StatusPagesSnapshotProvider : ISnapshotDataProvider
    {
        private readonly IMediator _mediator;

        public StatusPagesSnapshotProvider(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<List<MessageBase>> GetDataAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<MessageBase>();

            var statusPages = (await _mediator.Send(new StatusPagesQuery
            {
                Query = new(query => query
                    .Include(x => x.MonitorSummaries)
                    .ThenInclude(x => x.LabeledMonitors))
            }, cancellationToken))?.StatusPages;

            if (statusPages != null)
            {
                foreach (var statusPage in statusPages)
                {
                    result.Add(new CreateOrUpdateStatusPageCmd()
                    {
                        Data = statusPage
                    });
                }
            }

            return result;
        }

        [SnapshotApplyDataOrder(20)]
        public async Task ApplyDataAsync(List<MessageBase> data, CancellationToken cancellationToken = default)
        {
            var statusPages = (await _mediator.Send(new StatusPagesQuery(), cancellationToken))?.StatusPages;

            if (statusPages != null)
            {
                foreach (var statusPage in statusPages)
                {
                    //Local entity does not existing in the snapshot data from the leader anymore, remove it
                    if (!data.Any(x => x is CreateOrUpdateStatusPageCmd createOrUpdate && createOrUpdate.Data.Id == statusPage.Id))
                    {
                        await _mediator.Send(new DeleteStatusPageCmd()
                        {
                            StatusPageId = statusPage.Id
                        }, cancellationToken);
                    }
                }
            }

            foreach (var message in data)
            {
                switch (message)
                {
                    case CreateOrUpdateStatusPageCmd createOrUpdate:
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
