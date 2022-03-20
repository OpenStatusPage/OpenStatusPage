using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Monitoring.StatusTimeline.Commands
{
    public class FetchPendingChangesCmd : RequestBase<DateTimeOffset?>
    {
        public string MonitorId { get; set; }

        public DateTimeOffset Before { get; set; }

        public class Handler : IRequestHandler<FetchPendingChangesCmd, DateTimeOffset?>
        {
            private readonly StatusTimelineService _statusTimelineService;

            public Handler(StatusTimelineService statusTimelineService)
            {
                _statusTimelineService = statusTimelineService;
            }

            public async Task<DateTimeOffset?> Handle(FetchPendingChangesCmd request, CancellationToken cancellationToken)
            {
                return await _statusTimelineService.GetPendingChangeBeforeAsync(request.MonitorId, request.Before, cancellationToken);
            }
        }
    }
}
