using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Cluster.Consensus;
using OpenStatusPage.Server.Application.Notifications.Providers.Commands;

namespace OpenStatusPage.Server.Application.Notifications.Providers
{
    public class NotifictionProvidersSnapshotProvider : ISnapshotDataProvider
    {
        private readonly IMediator _mediator;

        public NotifictionProvidersSnapshotProvider(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<List<MessageBase>> GetDataAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<MessageBase>();

            var notificationProviders = (await _mediator.Send(new NotificationProvidersQuery(), cancellationToken))?.NotificationProviders;

            if (notificationProviders != null)
            {
                foreach (var notificationProvider in notificationProviders)
                {
                    result.Add(new CreateOrUpdateNotificationProviderCmd()
                    {
                        Data = notificationProvider
                    });
                }
            }

            return result;
        }

        [SnapshotApplyDataOrder(0)]
        public async Task ApplyDataAsync(List<MessageBase> data, CancellationToken cancellationToken = default)
        {
            var notificationProviders = (await _mediator.Send(new NotificationProvidersQuery(), cancellationToken))?.NotificationProviders;

            if (notificationProviders != null)
            {
                foreach (var notificationProvider in notificationProviders)
                {
                    //Local entity does not existing in the snapshot data from the leader anymore, remove it
                    if (!data.Any(x => x is CreateOrUpdateNotificationProviderCmd createOrUpdate && createOrUpdate.Data.Id == notificationProvider.Id))
                    {
                        await _mediator.Send(new DeleteNotificationProviderCmd()
                        {
                            ProviderId = notificationProvider.Id
                        }, cancellationToken);
                    }
                }
            }

            foreach (var message in data)
            {
                switch (message)
                {
                    case CreateOrUpdateNotificationProviderCmd createOrUpdate:
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
