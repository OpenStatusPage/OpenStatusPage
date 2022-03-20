using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Cluster.Consensus;
using OpenStatusPage.Server.Application.StatusHistory.Commands;
using OpenStatusPage.Server.Persistence;

namespace OpenStatusPage.Server.Application.StatusHistory
{
    public class StatusHistoriesSnapshotProvider : ISnapshotDataProvider
    {
        private readonly IMediator _mediator;

        public StatusHistoriesSnapshotProvider(IMediator mediator, ApplicationDbContext applicationDbContext)
        {
            _mediator = mediator;
        }

        public async Task<List<MessageBase>> GetDataAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<MessageBase>();

            var records = (await _mediator.Send(new StatusHistoriesQuery(), cancellationToken))?.HistoryRecords;

            if (records != null)
            {
                foreach (var record in records)
                {
                    result.Add(new CreateStatusHistoryRecordCmd()
                    {
                        MonitorId = record.MonitorId,
                        UtcFrom = record.FromUtc,
                        Status = record.Status
                    });
                }
            }

            return result;
        }

        [SnapshotApplyDataOrder(20)]
        public async Task ApplyDataAsync(List<MessageBase> data, CancellationToken cancellationToken = default)
        {
            var records = (await _mediator.Send(new StatusHistoriesQuery(), cancellationToken))?.HistoryRecords;

            if (records != null)
            {
                foreach (var record in records)
                {
                    //Local entity does not existing in the snapshot data from the leader anymore, remove it
                    if (!data.Any(x => x is CreateStatusHistoryRecordCmd createOrUpdate &&
                        createOrUpdate.MonitorId == record.MonitorId &&
                        createOrUpdate.UtcFrom == record.FromUtc &&
                        createOrUpdate.Status == record.Status))
                    {
                        await _mediator.Send(new DeleteStatusHistoryRecordCmd
                        {
                            MonitorId = record.MonitorId,
                            UtcFrom = record.FromUtc
                        }, cancellationToken);
                    }
                }
            }

            foreach (var message in data)
            {
                switch (message)
                {
                    case CreateStatusHistoryRecordCmd createOrUpdate:
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
