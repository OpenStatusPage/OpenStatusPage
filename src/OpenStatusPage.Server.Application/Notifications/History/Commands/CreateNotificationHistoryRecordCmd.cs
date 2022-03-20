using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Notifications.History.Commands
{
    public class CreateNotificationHistoryRecordCmd : MessageBase
    {
        public string MonitorId { get; set; }

        public DateTime StatusUtc { get; set; }

        public class Handler : IRequestHandler<CreateNotificationHistoryRecordCmd>
        {
            private readonly NotificationHistoryService _notificationHistoryService;

            public Handler(NotificationHistoryService notificationHistoryService)
            {
                _notificationHistoryService = notificationHistoryService;
            }

            public async Task<Unit> Handle(CreateNotificationHistoryRecordCmd request, CancellationToken cancellationToken)
            {
                //Check if notification was already marked as sent
                if (!await _notificationHistoryService.Get().AnyAsync(x => x.MonitorId == request.MonitorId && x.StatusUtc >= request.StatusUtc, cancellationToken))
                {
                    try
                    {
                        await _notificationHistoryService.CreateAsync(new()
                        {
                            MonitorId = request.MonitorId,
                            StatusUtc = request.StatusUtc,
                        });
                    }
                    catch
                    {
                        //Catch in case record already exists in a shared db
                    }
                }

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<CreateNotificationHistoryRecordCmd>
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
