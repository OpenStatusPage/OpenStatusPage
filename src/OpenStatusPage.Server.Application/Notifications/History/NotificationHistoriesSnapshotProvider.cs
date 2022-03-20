using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Cluster.Consensus;
using OpenStatusPage.Server.Application.Notifications.History.Commands;
using OpenStatusPage.Server.Persistence;


namespace OpenStatusPage.Server.Application.Notifications.History
{
    public class NotificationHistoriesSnapshotProvider : ISnapshotDataProvider
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _applicationDbContext;

        public NotificationHistoriesSnapshotProvider(IMediator mediator, ApplicationDbContext applicationDbContext)
        {
            _mediator = mediator;
            _applicationDbContext = applicationDbContext;
        }

        public async Task<List<MessageBase>> GetDataAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<MessageBase>();

            var records = (await _mediator.Send(new NotificationHistoriesQuery(), cancellationToken))?.NotificationHistoryRecords;

            if (records != null)
            {
                foreach (var record in records)
                {
                    result.Add(new CreateNotificationHistoryRecordCmd()
                    {
                        MonitorId = record.MonitorId,
                        StatusUtc = record.StatusUtc
                    });
                }
            }

            return result;
        }

        [SnapshotApplyDataOrder(20)]
        public async Task ApplyDataAsync(List<MessageBase> data, CancellationToken cancellationToken = default)
        {
            var records = await _applicationDbContext.NotificationHistoryRecords.ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                //Local entity does not existing in the snapshot data from the leader anymore, remove it
                if (!data.Any(x => x is CreateNotificationHistoryRecordCmd createOrUpdate &&
                    createOrUpdate.MonitorId == record.MonitorId &&
                    createOrUpdate.StatusUtc == record.StatusUtc))
                {
                    _applicationDbContext.Remove(record);
                }
            }

            await _applicationDbContext.SaveChangesAsync(cancellationToken);

            foreach (var message in data)
            {
                switch (message)
                {
                    case CreateNotificationHistoryRecordCmd createOrUpdate:
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
