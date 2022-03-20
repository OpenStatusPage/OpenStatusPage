using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Server.Application.StatusHistory.Commands
{
    public class CreateStatusHistoryRecordCmd : MessageBase
    {
        public string MonitorId { get; set; }

        public DateTime UtcFrom { get; set; }

        public ServiceStatus Status { get; set; }

        public class Handler : IRequestHandler<CreateStatusHistoryRecordCmd>
        {
            private readonly StatusHistoryService _statusHistoryService;

            public Handler(StatusHistoryService statusHistoryService)
            {
                _statusHistoryService = statusHistoryService;
            }

            public async Task<Unit> Handle(CreateStatusHistoryRecordCmd request, CancellationToken cancellationToken)
            {
                //Check if timeline item already exists
                if (!await _statusHistoryService.Get().AnyAsync(x => x.MonitorId == request.MonitorId && x.FromUtc == request.UtcFrom, cancellationToken))
                {
                    try
                    {
                        await _statusHistoryService.CreateAsync(new()
                        {
                            MonitorId = request.MonitorId,
                            FromUtc = request.UtcFrom,
                            Status = request.Status,
                        });
                    }
                    catch
                    {
                        //Catch in case of existing entry in shared dbs
                    }
                }

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<CreateStatusHistoryRecordCmd>
        {
            public Validator()
            {
                RuleFor(x => x.MonitorId)
                    .NotEmpty()
                    .WithMessage("Field MonitorId is required");
            }
        }
    }
}
