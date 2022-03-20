using FluentValidation;
using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Monitoring.Coordination.Commands
{
    public class TaskAssignmentCmd : MessageBase
    {
        public string Id { get; set; }

        public DateTimeOffset DateTime { get; set; }

        public string MonitorId { get; set; }

        public long MonitorVersion { get; set; }

        public HashSet<string> WorkerIds { get; set; }

        public class Handler : IRequestHandler<TaskAssignmentCmd>
        {
            private readonly TaskCoordinationService _taskCoordinationService;

            public Handler(TaskCoordinationService taskCoordinationService)
            {
                _taskCoordinationService = taskCoordinationService;
            }

            public async Task<Unit> Handle(TaskAssignmentCmd request, CancellationToken cancellationToken)
            {
                await _taskCoordinationService.ApplyTaskAssignmentAsync(request, cancellationToken);

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<TaskAssignmentCmd>
        {
            public Validator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty()
                    .WithMessage("Field Id is required.");

                RuleFor(x => x.MonitorId)
                    .NotEmpty()
                    .WithMessage("Field MonitorId is required.");

                RuleFor(x => x.MonitorVersion)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Ivalid value for field MonitorVersion. Allowed values >= 0.");

                RuleFor(x => x.WorkerIds)
                    .NotEmpty()
                    .WithMessage("Field WorkerIds is required and can not be empty.");
            }
        }
    }
}
