using MediatR;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitoring.StatusTimeline.Commands;

namespace OpenStatusPage.Server.Application.Notifications.PostProcessors
{
    public class StatusChangedNotificationsProcessor : IRequestPostProcessor<UpdateServiceStatusCmd>
    {
        private readonly NotificationService _notificationService;

        public StatusChangedNotificationsProcessor(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Process(UpdateServiceStatusCmd request, Unit response, CancellationToken cancellationToken)
        {
            _notificationService.DispatchNotifications();
        }
    }
}
