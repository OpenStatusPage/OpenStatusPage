using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Notifications.History.Commands
{
    public class DeleteNotificationHistoryRecordCmd : MessageBase
    {
        public string MonitorId { get; set; }

        public DateTime StatusUtc { get; set; }

        public class Handler : IRequestHandler<DeleteNotificationHistoryRecordCmd>
        {
            private readonly NotificationHistoryService _notificationHistoryService;

            public Handler(NotificationHistoryService notificationHistoryService)
            {
                _notificationHistoryService = notificationHistoryService;
            }

            public async Task<Unit> Handle(DeleteNotificationHistoryRecordCmd request, CancellationToken cancellationToken)
            {
                var record = await _notificationHistoryService.Get()
                    .FirstOrDefaultAsync(x => x.MonitorId == request.MonitorId && x.StatusUtc == request.StatusUtc, cancellationToken);

                //Already deleted
                if (record == null) return Unit.Value;

                await _notificationHistoryService.DeleteAsync(record);

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<DeleteNotificationHistoryRecordCmd>
        {
            public Validator()
            {
                RuleFor(x => x.MonitorId)
                    .NotEmpty()
                    .WithMessage("Field MonitorId is required.");
            }
        }
    }
}
