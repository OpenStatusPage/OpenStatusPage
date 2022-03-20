using FluentValidation;
using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Server.Application.Monitoring.StatusTimeline.Commands
{
    public class UpdateServiceStatusCmd : MessageBase
    {
        public string MonitorId { get; set; }

        public DateTimeOffset DateTime { get; set; }

        public ServiceStatus ServiceStatus { get; set; }

        public string? TaskAssignmentId { get; set; }

        public class Handler : IRequestHandler<UpdateServiceStatusCmd>
        {
            private readonly StatusTimelineService _timelineService;

            public Handler(StatusTimelineService timelineService)
            {
                _timelineService = timelineService;
            }

            public async Task<Unit> Handle(UpdateServiceStatusCmd request, CancellationToken cancellationToken)
            {
                await _timelineService.HandleServiceStatusUpdateAsync(request.MonitorId, request.DateTime, request.ServiceStatus, cancellationToken);

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<UpdateServiceStatusCmd>
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
