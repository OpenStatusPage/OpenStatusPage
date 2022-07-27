using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.StatusHistory.Commands
{
    public class DeleteStatusHistoryRecordCmd : MessageBase
    {
        public string MonitorId { get; set; }

        public DateTime UtcFrom { get; set; }

        public class Handler : IRequestHandler<DeleteStatusHistoryRecordCmd>
        {
            private readonly StatusHistoryService _statusHistoryService;

            public Handler(StatusHistoryService statusHistoryService)
            {
                _statusHistoryService = statusHistoryService;
            }

            public async Task<Unit> Handle(DeleteStatusHistoryRecordCmd request, CancellationToken cancellationToken)
            {
                var incident = await _statusHistoryService.Get()
                    .FirstOrDefaultAsync(x => x.MonitorId == request.MonitorId && x.FromUtc == request.UtcFrom, cancellationToken);

                //Already deleted
                if (incident == null) return Unit.Value;

                await _statusHistoryService.DeleteAsync(incident);

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<DeleteStatusHistoryRecordCmd>
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
