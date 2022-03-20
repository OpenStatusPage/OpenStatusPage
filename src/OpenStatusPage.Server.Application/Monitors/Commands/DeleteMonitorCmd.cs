using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc.Attributes;

namespace OpenStatusPage.Server.Application.Monitors.Commands
{
    [RequiresDbTransaction]
    public class DeleteMonitorCmd : MessageBase
    {
        public string MonitorId { get; set; }

        public class Handler : IRequestHandler<DeleteMonitorCmd>
        {
            private readonly MonitorService _monitorService;

            public Handler(MonitorService monitorService)
            {
                _monitorService = monitorService;
            }

            public async Task<Unit> Handle(DeleteMonitorCmd request, CancellationToken cancellationToken)
            {
                var monitor = await _monitorService.Get(request.MonitorId).FirstOrDefaultAsync(cancellationToken);

                //Already deleted
                if (monitor == null) return Unit.Value;

                await _monitorService.DeleteAsync(monitor);

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<DeleteMonitorCmd>
        {
            public Validator()
            {
                RuleFor(x => x.MonitorId)
                    .NotEmpty()
                    .WithMessage("Missing MonitorId");
            }
        }
    }
}
