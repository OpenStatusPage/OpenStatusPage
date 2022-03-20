using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.StatusHistory.Commands;

namespace OpenStatusPage.Server.Application.Notifications.History.PostProcessors
{
    public class HistoryRecordDeletedProcessor : IRequestPostProcessor<DeleteStatusHistoryRecordCmd>
    {
        private readonly NotificationHistoryService _notificationHistoryService;

        public HistoryRecordDeletedProcessor(NotificationHistoryService notificationHistoryService)
        {
            _notificationHistoryService = notificationHistoryService;
        }

        public async Task Process(DeleteStatusHistoryRecordCmd request, Unit response, CancellationToken cancellationToken)
        {
            var record = await _notificationHistoryService.Get()
                .FirstOrDefaultAsync(x => x.MonitorId == request.MonitorId && x.StatusUtc == request.UtcFrom, cancellationToken);

            if (record == null) return;

            await _notificationHistoryService.DeleteAsync(record);
        }
    }
}
