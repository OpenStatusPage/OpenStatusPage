using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Domain.Entities.StatusHistory;

namespace OpenStatusPage.Server.Application.StatusHistory.Commands
{
    public class GetStatusFromHistoryCmd : RequestBase<StatusHistoryRecord?>
    {
        public string MonitorId { get; set; }

        public DateTime UtcAt { get; set; }

        public class Handler : IRequestHandler<GetStatusFromHistoryCmd, StatusHistoryRecord?>
        {
            private readonly StatusHistoryService _statusHistoryService;

            public Handler(StatusHistoryService statusHistoryService)
            {
                _statusHistoryService = statusHistoryService;
            }

            public async Task<StatusHistoryRecord?> Handle(GetStatusFromHistoryCmd request, CancellationToken cancellationToken)
            {
                var record = await _statusHistoryService.Get()
                    .Where(x => x.MonitorId == request.MonitorId)
                    .OrderByDescending(x => x.FromUtc)
                    .FirstOrDefaultAsync(x => request.UtcAt >= x.FromUtc, cancellationToken);

                return record;
            }
        }
    }
}
