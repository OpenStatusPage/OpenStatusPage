using FluentValidation;
using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Server.Application.Monitoring.StatusTimeline.Commands
{
    public class AddLocalServiceStatusResultCmd : MessageBase
    {
        public string MonitorId { get; set; }

        public long MonitorVersion { get; set; }

        public DateTimeOffset DateTime { get; set; }

        public ServiceStatus ServiceStatus { get; set; }

        public class Handler : IRequestHandler<AddLocalServiceStatusResultCmd>
        {
            private readonly StatusTimelineService _timelineService;

            public Handler(StatusTimelineService timelineService)
            {
                _timelineService = timelineService;
            }

            public async Task<Unit> Handle(AddLocalServiceStatusResultCmd request, CancellationToken cancellationToken)
            {
                await _timelineService.AddLocalServiceStatusResultAsync(request.MonitorId, request.MonitorVersion, request.DateTime, request.ServiceStatus, cancellationToken);

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<AddLocalServiceStatusResultCmd>
        {
            public Validator()
            {
                RuleFor(x => x.MonitorId)
                    .NotEmpty()
                    .WithMessage("Field MonitorId is required.");

                RuleFor(x => x.MonitorVersion)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Invalid value for field MonitorVersion.");
            }
        }
    }
}
